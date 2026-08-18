using System;
using System.Collections.Generic;

namespace TwinParticles.CheckEngine.Domain.Marketplace;

public sealed class VendorOrderLine
{
    public int OrderItemId { get; init; }

    public int ProductId { get; init; }

    public int Quantity { get; init; }

    public decimal PriceExclTax { get; init; }
}
