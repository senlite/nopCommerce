namespace TwinParticles.CheckEngine.Domain.Dealer;

public sealed class DealerTerritory
{
    public int Id { get; set; }

    public int DealerAccountId { get; set; }

    public int? MarketId { get; set; }

    public string? RegionCode { get; set; }
}
