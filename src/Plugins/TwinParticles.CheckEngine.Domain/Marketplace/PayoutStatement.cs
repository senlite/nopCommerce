using System;
using System.Collections.Generic;

namespace TwinParticles.CheckEngine.Domain.Marketplace;

public sealed class PayoutStatement
{
    public int Id { get; set; }

    public int VendorId { get; set; }

    public DateTime PeriodStartUtc { get; set; }

    public DateTime PeriodEndUtc { get; set; }

    public decimal GrossSales { get; set; }

    public decimal TotalCommission { get; set; }

    public decimal TotalRefunds { get; set; }

    public decimal TotalAdjustments { get; set; }

    public decimal NetPayout { get; set; }

    public PayoutStatementStatus Status { get; set; } = PayoutStatementStatus.Draft;

    public string? ErpReferenceId { get; set; }

    public decimal? ErpReportedTotal { get; set; }

    public DateTime CreatedUtc { get; set; }

    public DateTime? FinalizedUtc { get; set; }

    public IReadOnlyList<PayoutStatementLine> Lines { get; set; } = [];
}

public sealed class PayoutStatementLine
{
    public int Id { get; set; }

    public int StatementId { get; set; }

    public int OrderId { get; set; }

    public int OrderItemId { get; set; }

    public decimal LineSubtotalExclTax { get; set; }

    public decimal CommissionAmount { get; set; }

    public decimal RefundAmount { get; set; }
}

public sealed class PayoutAdjustment
{
    public int Id { get; set; }

    public int VendorId { get; set; }

    public int? StatementId { get; set; }

    public decimal Amount { get; set; }

    public string ReasonCode { get; set; } = string.Empty;

    public string? Notes { get; set; }

    public string CreatedBy { get; set; } = string.Empty;

    public DateTime CreatedUtc { get; set; }
}

public sealed class PayoutSourceLine
{
    public int OrderId { get; init; }

    public int OrderItemId { get; init; }

    public decimal LineSubtotalExclTax { get; init; }

    public decimal CommissionAmount { get; init; }

    public decimal RefundAmount { get; init; }

    public DateTime OrderCreatedUtc { get; init; }
}

public sealed class PayoutReconciliationResult
{
    public bool Succeeded { get; init; }

    public decimal LocalNetPayout { get; init; }

    public decimal ErpTotal { get; init; }

    public decimal Variance { get; init; }

    public bool WithinTolerance { get; init; }

    public static PayoutReconciliationResult Fail(decimal local, decimal erp)
        => new()
        {
            Succeeded = false,
            LocalNetPayout = local,
            ErpTotal = erp,
            Variance = Math.Abs(local - erp),
            WithinTolerance = false
        };
}
