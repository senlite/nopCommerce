using System;
using System.Collections.Generic;
using System.Linq;
using TwinParticles.CheckEngine.Domain.Marketplace;

namespace TwinParticles.CheckEngine.Application.Marketplace;

/// <summary>
/// Pure FR-853 commission evaluation. Rules are data-driven; callers snapshot results at order time.
/// </summary>
public sealed class CommissionEvaluationService
{
    public CommissionEvaluationResult EvaluateLine(CommissionPlan plan, CommissionLineContext context)
    {
        ArgumentNullException.ThrowIfNull(plan);
        ArgumentNullException.ThrowIfNull(context);

        var rules = plan.Rules
            .Where(rule => rule.IsActive && IsEffective(rule, context.OrderUtc))
            .OrderBy(rule => rule.Priority)
            .ThenBy(rule => rule.Id)
            .ToList();

        foreach (var rule in rules)
        {
            var result = TryEvaluateRule(rule, context);
            if (result.Matched)
                return result;
        }

        return CommissionEvaluationResult.None;
    }

    private static bool IsEffective(CommissionRule rule, DateTime orderUtc)
    {
        if (rule.EffectiveFromUtc.HasValue && orderUtc < rule.EffectiveFromUtc.Value)
            return false;
        if (rule.EffectiveToUtc.HasValue && orderUtc > rule.EffectiveToUtc.Value)
            return false;
        return true;
    }

    private static CommissionEvaluationResult TryEvaluateRule(CommissionRule rule, CommissionLineContext context)
    {
        return rule.ModelKind switch
        {
            CommissionModelKind.CategoryOverride when rule.CategoryId.HasValue
                && context.CategoryIds.Contains(rule.CategoryId.Value)
                => EvaluateAmountRule(rule, context),
            CommissionModelKind.CategoryOverride => CommissionEvaluationResult.None,
            CommissionModelKind.Tiered => EvaluateTiered(rule, context),
            CommissionModelKind.Percentage => EvaluateAmountRule(rule, context),
            CommissionModelKind.Flat => EvaluateAmountRule(rule, context),
            _ => CommissionEvaluationResult.None
        };
    }

    private static CommissionEvaluationResult EvaluateTiered(CommissionRule rule, CommissionLineContext context)
    {
        var band = rule.TierBands
            .OrderBy(b => b.MinVolume)
            .FirstOrDefault(b =>
                context.VendorPeriodVolumeExclTax >= b.MinVolume
                && (!b.MaxVolume.HasValue || context.VendorPeriodVolumeExclTax < b.MaxVolume.Value));

        if (band is null)
            return CommissionEvaluationResult.None;

        var amount = RoundCurrency(context.LineSubtotalExclTax * band.PercentageRate);
        return BuildResult(rule, CommissionBasis.LineSubtotal, band.PercentageRate, amount);
    }

    private static CommissionEvaluationResult EvaluateAmountRule(CommissionRule rule, CommissionLineContext context)
    {
        if (rule.ModelKind is CommissionModelKind.Percentage or CommissionModelKind.CategoryOverride)
        {
            if (!rule.PercentageRate.HasValue || rule.PercentageRate.Value <= 0)
                return CommissionEvaluationResult.None;

            var basisAmount = rule.Basis switch
            {
                CommissionBasis.OrderSubtotal => context.OrderSubtotalExclTax,
                CommissionBasis.LineSubtotal => context.LineSubtotalExclTax,
                CommissionBasis.PerLineItem => context.Quantity,
                CommissionBasis.PerOrder => 1m,
                _ => context.LineSubtotalExclTax
            };

            var amount = rule.Basis is CommissionBasis.PerLineItem or CommissionBasis.PerOrder
                ? RoundCurrency(basisAmount * rule.PercentageRate.Value)
                : RoundCurrency(basisAmount * rule.PercentageRate.Value);

            return BuildResult(rule, rule.Basis, rule.PercentageRate.Value, amount);
        }

        if (!rule.FlatAmount.HasValue || rule.FlatAmount.Value <= 0)
            return CommissionEvaluationResult.None;

        if (rule.Basis == CommissionBasis.PerOrder && context.FlatPerOrderAlreadyApplied)
            return CommissionEvaluationResult.None;

        var flatAmount = rule.Basis switch
        {
            CommissionBasis.PerLineItem => RoundCurrency(rule.FlatAmount.Value * context.Quantity),
            CommissionBasis.PerOrder => RoundCurrency(rule.FlatAmount.Value),
            CommissionBasis.LineSubtotal => RoundCurrency(rule.FlatAmount.Value),
            CommissionBasis.OrderSubtotal => RoundCurrency(rule.FlatAmount.Value),
            _ => RoundCurrency(rule.FlatAmount.Value)
        };

        return BuildResult(rule, rule.Basis, rule.FlatAmount.Value, flatAmount);
    }

    private static CommissionEvaluationResult BuildResult(
        CommissionRule rule,
        CommissionBasis basis,
        decimal rateApplied,
        decimal amount)
        => new()
        {
            Matched = true,
            RuleId = rule.Id,
            ModelKind = rule.ModelKind,
            Basis = basis,
            RateApplied = rateApplied,
            CommissionAmount = amount
        };

    private static decimal RoundCurrency(decimal value)
        => Math.Round(value, 4, MidpointRounding.AwayFromZero);
}
