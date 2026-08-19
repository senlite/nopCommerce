namespace TwinParticles.CheckEngine.Domain.Workshop;

/// <summary>
/// Volume discount band for a trade price list (FR-1021).
/// </summary>
public sealed class WorkshopPriceTier
{
    public int Id { get; set; }

    public int PriceListId { get; set; }

    public int MinQuantity { get; set; }

    public decimal DiscountPercent { get; set; }
}
