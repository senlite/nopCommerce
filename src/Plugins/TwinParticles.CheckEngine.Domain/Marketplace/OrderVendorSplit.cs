using System;
using System.Collections.Generic;

namespace TwinParticles.CheckEngine.Domain.Marketplace;

/// <summary>
/// FR-855 / AC-19.2: one checkout group links a parent nopCommerce order to per-vendor splits.
/// </summary>
public sealed class OrderCheckoutGroup
{
    public Guid CheckoutGroupId { get; init; }

    public int ParentOrderId { get; init; }

    public DateTime CreatedUtc { get; init; }

    public IReadOnlyList<OrderVendorSplit> Splits { get; init; } = [];
}

public sealed class OrderVendorSplit
{
    public int Id { get; init; }

    public Guid CheckoutGroupId { get; init; }

    public int ParentOrderId { get; init; }

    public int VendorId { get; init; }

    public decimal LineSubtotalExclTax { get; init; }

    public IReadOnlyList<OrderVendorSplitLine> Lines { get; init; } = [];
}

public sealed class OrderVendorSplitLine
{
    public int Id { get; init; }

    public int SplitId { get; init; }

    public int OrderItemId { get; init; }

    public int ProductId { get; init; }

    public int Quantity { get; init; }

    public decimal LineSubtotalExclTax { get; init; }
}

public sealed class ShipmentVendorMap
{
    public int Id { get; init; }

    public int ShipmentId { get; init; }

    public int OrderId { get; init; }

    public int VendorId { get; init; }

    public DateTime CreatedUtc { get; init; }
}
