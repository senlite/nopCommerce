namespace TwinParticles.CheckEngine.Domain.Portals;

public sealed class PortalOrderLine
{
    public int ProductId { get; init; }

    public int Quantity { get; init; }

    public decimal UnitPrice { get; init; }
}
