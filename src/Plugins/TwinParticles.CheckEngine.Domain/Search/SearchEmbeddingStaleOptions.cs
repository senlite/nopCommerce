namespace TwinParticles.CheckEngine.Domain.Search;

/// <summary>
/// Optional staleness criteria when loading embedding catalog rows for incremental refresh.
/// </summary>
public sealed class SearchEmbeddingStaleOptions
{
    public string? ExpectedModelHash { get; init; }
}
