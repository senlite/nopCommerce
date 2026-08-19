namespace TwinParticles.CheckEngine.Domain.Workshop;

/// <summary>
/// Account-level trade discount band (FR-1021).
/// </summary>
public sealed class WorkshopAccountTier
{
    public int Id { get; set; }

    public string TierCode { get; set; } = string.Empty;

    public decimal DiscountPercent { get; set; }

    public string? Label { get; set; }
}
