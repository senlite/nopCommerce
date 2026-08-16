namespace TwinParticles.CheckEngine.Domain.Search;

public sealed class SearchVinCandidate
{
    public int VehicleConfigurationId { get; init; }

    public string? Label { get; init; }

    public int? ModelYear { get; init; }

    public decimal Confidence { get; init; }
}
