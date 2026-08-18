using System;
using System.Collections.Generic;
using FluentAssertions;
using NUnit.Framework;
using TwinParticles.CheckEngine.Application.Marketplace;
using TwinParticles.CheckEngine.Domain.Marketplace;

namespace TwinParticles.CheckEngine.Tests.Architecture;

[TestFixture]
public class CommissionEvaluationServiceTests
{
    private readonly CommissionEvaluationService _service = new();

    [Test]
    public void Percentage_Should_Apply_To_Line_Subtotal()
    {
        var plan = Plan(
            Rule(1, CommissionModelKind.Percentage, CommissionBasis.LineSubtotal, percentage: 0.10m));

        var result = _service.EvaluateLine(plan, Context(lineSubtotal: 100m));

        result.Matched.Should().BeTrue();
        result.CommissionAmount.Should().Be(10m);
        result.RateApplied.Should().Be(0.10m);
    }

    [Test]
    public void Flat_PerLineItem_Should_Multiply_By_Quantity()
    {
        var plan = Plan(
            Rule(1, CommissionModelKind.Flat, CommissionBasis.PerLineItem, flat: 2.5m));

        var result = _service.EvaluateLine(plan, Context(lineSubtotal: 100m, quantity: 4));

        result.CommissionAmount.Should().Be(10m);
    }

    [Test]
    public void Flat_PerOrder_Should_Apply_Once()
    {
        var plan = Plan(
            Rule(1, CommissionModelKind.Flat, CommissionBasis.PerOrder, flat: 5m));

        var first = _service.EvaluateLine(plan, Context(flatAlreadyApplied: false));
        var second = _service.EvaluateLine(plan, Context(flatAlreadyApplied: true));

        first.CommissionAmount.Should().Be(5m);
        second.Matched.Should().BeFalse();
    }

    [Test]
    public void Tiered_Should_Select_Band_By_Period_Volume()
    {
        var plan = Plan(Rule(1, CommissionModelKind.Tiered, CommissionBasis.LineSubtotal, tierBands:
        [
            new CommissionTierBand { MinVolume = 0m, MaxVolume = 1000m, PercentageRate = 0.05m },
            new CommissionTierBand { MinVolume = 1000m, MaxVolume = null, PercentageRate = 0.08m }
        ]));

        var lowTier = _service.EvaluateLine(plan, Context(lineSubtotal: 200m, periodVolume: 500m));
        var highTier = _service.EvaluateLine(plan, Context(lineSubtotal: 200m, periodVolume: 1500m));

        lowTier.RateApplied.Should().Be(0.05m);
        lowTier.CommissionAmount.Should().Be(10m);
        highTier.RateApplied.Should().Be(0.08m);
        highTier.CommissionAmount.Should().Be(16m);
    }

    [Test]
    public void CategoryOverride_Should_Beat_Default_Percentage()
    {
        var plan = Plan(
            Rule(1, CommissionModelKind.CategoryOverride, CommissionBasis.LineSubtotal, categoryId: 20, percentage: 0.15m, priority: 1),
            Rule(2, CommissionModelKind.Percentage, CommissionBasis.LineSubtotal, percentage: 0.05m, priority: 100));

        var cooling = _service.EvaluateLine(plan, Context(lineSubtotal: 100m, categoryIds: [20]));
        var other = _service.EvaluateLine(plan, Context(lineSubtotal: 100m, categoryIds: [99]));

        cooling.ModelKind.Should().Be(CommissionModelKind.CategoryOverride);
        cooling.CommissionAmount.Should().Be(15m);
        other.CommissionAmount.Should().Be(5m);
    }

    [Test]
    public void Changed_Rule_After_Order_Should_Not_Alter_Snapshotted_Rate()
    {
        var planAtOrder = Plan(
            Rule(1, CommissionModelKind.Percentage, CommissionBasis.LineSubtotal, percentage: 0.10m));
        var snapshot = _service.EvaluateLine(planAtOrder, Context(lineSubtotal: 100m));

        var planLater = Plan(
            Rule(1, CommissionModelKind.Percentage, CommissionBasis.LineSubtotal, percentage: 0.20m));
        var live = _service.EvaluateLine(planLater, Context(lineSubtotal: 100m));

        snapshot.RateApplied.Should().Be(0.10m);
        snapshot.CommissionAmount.Should().Be(10m);
        live.CommissionAmount.Should().Be(20m);
        snapshot.RateApplied.Should().NotBe(live.RateApplied);
    }

    private static CommissionPlan Plan(params CommissionRule[] rules)
        => new() { Id = 1, VendorId = 1, Rules = rules };

    private static CommissionRule Rule(
        int id,
        CommissionModelKind kind,
        CommissionBasis basis,
        decimal? flat = null,
        decimal? percentage = null,
        int? categoryId = null,
        int priority = 1,
        IReadOnlyList<CommissionTierBand>? tierBands = null)
        => new()
        {
            Id = id,
            PlanId = 1,
            ModelKind = kind,
            Basis = basis,
            Priority = priority,
            FlatAmount = flat,
            PercentageRate = percentage,
            CategoryId = categoryId,
            TierBands = tierBands ?? [],
            IsActive = true
        };

    private static CommissionLineContext Context(
        decimal lineSubtotal = 100m,
        int quantity = 1,
        IReadOnlyList<int>? categoryIds = null,
        decimal periodVolume = 0m,
        bool flatAlreadyApplied = false)
        => new()
        {
            VendorId = 1,
            ProductId = 100,
            CategoryIds = categoryIds ?? [],
            Quantity = quantity,
            LineSubtotalExclTax = lineSubtotal,
            OrderSubtotalExclTax = lineSubtotal,
            VendorPeriodVolumeExclTax = periodVolume,
            OrderUtc = DateTime.UtcNow,
            FlatPerOrderAlreadyApplied = flatAlreadyApplied
        };
}
