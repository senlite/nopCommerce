using System.Collections.Generic;

namespace TwinParticles.CheckEngine.Domain.Marketplace;

public sealed class VendorOrderRecord
{
    public int OrderId { get; init; }

    public int CustomerId { get; init; }

    public IReadOnlyList<int> ProductIds { get; init; } = [];
}
