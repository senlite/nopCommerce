using TwinParticles.CheckEngine.Domain.Search;

namespace TwinParticles.CheckEngine.Application.Search;

public sealed class RecommendationResult
{
    public bool VehicleScoped { get; init; }

    public IReadOnlyList<SearchHit> Hits { get; init; } = [];
}
