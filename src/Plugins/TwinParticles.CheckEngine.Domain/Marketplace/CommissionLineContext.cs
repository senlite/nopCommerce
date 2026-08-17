using System;
using System.Collections.Generic;

namespace TwinParticles.CheckEngine.Domain.Marketplace;

public sealed class CommissionLineContext
{
    public int VendorId { get; init; }

    public int ProductId { get; init; }

    public IReadOnlyList<int> CategoryIds { get; init; } = [];

    public int Quantity { get; init; }

    public decimal LineSubtotalExclTax { get; init; }

    public decimal OrderSubtotalExclTax { get; init; }

    public decimal VendorPeriodVolumeExclTax { get; init; }

    public DateTime OrderUtc { get; init; }

    public bool FlatPerOrderAlreadyApplied { get; init; }
}

public sealed class CommissionEvaluationResult
{
    public bool Matched { get; init; }

    public int RuleId { get; init; }

    public CommissionModelKind ModelKind { get; init; }

    public CommissionBasis Basis { get; init; }

    /// <summary>Flat currency amount or percentage rate (0.15 = 15%).</summary>
    public decimal RateApplied { get; init; }

    public decimal CommissionAmount { get; init; }

    public static CommissionEvaluationResult None { get; } = new() { Matched = false };
}
