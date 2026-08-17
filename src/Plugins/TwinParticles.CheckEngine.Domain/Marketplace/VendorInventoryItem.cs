namespace TwinParticles.CheckEngine.Domain.Marketplace;

public sealed class VendorInventoryItem
{
    public int ProductId { get; init; }

    public string Name { get; init; } = string.Empty;

    public int StockQuantity { get; init; }

    public bool Published { get; init; }
}
