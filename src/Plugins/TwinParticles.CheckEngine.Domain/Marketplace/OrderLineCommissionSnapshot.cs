using System;

namespace TwinParticles.CheckEngine.Domain.Marketplace;

/// <summary>
/// Immutable commission terms snapshotted at order placement (FR-853 / AC-19.6).
/// </summary>
public sealed class OrderLineCommissionSnapshot
{
    public int Id { get; set; }

    public int OrderId { get; set; }

    public int OrderItemId { get; set; }

    public int VendorId { get; set; }

    public int RuleId { get; set; }

    public CommissionModelKind ModelKind { get; set; }

    public CommissionBasis Basis { get; set; }

    public decimal RateApplied { get; set; }

    public decimal CommissionAmount { get; set; }

    public decimal LineSubtotalExclTax { get; set; }

    public int Quantity { get; set; }

    public DateTime SnapshottedUtc { get; set; }
}
