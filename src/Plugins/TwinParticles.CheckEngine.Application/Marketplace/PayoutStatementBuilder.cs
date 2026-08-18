using System;
using System.Collections.Generic;
using System.Linq;
using TwinParticles.CheckEngine.Domain.Marketplace;

namespace TwinParticles.CheckEngine.Application.Marketplace;

/// <summary>
/// FR-854: periodic statement = sales − commission − adjustments − refunds.
/// </summary>
public sealed class PayoutStatementBuilder
{
    public PayoutStatement Build(
        int vendorId,
        DateTime periodStartUtc,
        DateTime periodEndUtc,
        IReadOnlyList<PayoutSourceLine> sourceLines,
        IReadOnlyList<PayoutAdjustment> adjustments)
    {
        var lines = sourceLines.Select(source => new PayoutStatementLine
        {
            OrderId = source.OrderId,
            OrderItemId = source.OrderItemId,
            LineSubtotalExclTax = source.LineSubtotalExclTax,
            CommissionAmount = source.CommissionAmount,
            RefundAmount = source.RefundAmount
        }).ToList();

        var grossSales = lines.Sum(line => line.LineSubtotalExclTax);
        var totalCommission = lines.Sum(line => line.CommissionAmount);
        var totalRefunds = lines.Sum(line => line.RefundAmount);
        var totalAdjustments = adjustments.Sum(adj => adj.Amount);
        var netPayout = grossSales - totalCommission - totalRefunds + totalAdjustments;

        return new PayoutStatement
        {
            VendorId = vendorId,
            PeriodStartUtc = periodStartUtc,
            PeriodEndUtc = periodEndUtc,
            GrossSales = Round(grossSales),
            TotalCommission = Round(totalCommission),
            TotalRefunds = Round(totalRefunds),
            TotalAdjustments = Round(totalAdjustments),
            NetPayout = Round(netPayout),
            Status = PayoutStatementStatus.Draft,
            CreatedUtc = DateTime.UtcNow,
            Lines = lines
        };
    }

    public PayoutReconciliationResult Reconcile(decimal localNetPayout, decimal erpTotal, decimal tolerance = 0.01m)
    {
        var variance = Math.Abs(localNetPayout - erpTotal);
        var within = variance <= tolerance;
        return new PayoutReconciliationResult
        {
            Succeeded = within,
            LocalNetPayout = localNetPayout,
            ErpTotal = erpTotal,
            Variance = variance,
            WithinTolerance = within
        };
    }

    private static decimal Round(decimal value)
        => Math.Round(value, 4, MidpointRounding.AwayFromZero);
}
