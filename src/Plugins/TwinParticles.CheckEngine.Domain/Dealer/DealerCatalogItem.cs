namespace TwinParticles.CheckEngine.Domain.Dealer;

public sealed class DealerCatalogItem
{
    public int ProductId { get; set; }

    public string Sku { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public decimal DealerPrice { get; set; }

    public int RemainingAllocationUnits { get; set; }
}
