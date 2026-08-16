namespace TwinParticles.CheckEngine.Application.Search;

public sealed class SearchEmbeddingRebuildResult
{
    public string Locale { get; init; } = "en";

    public int CatalogCount { get; init; }

    public int Indexed { get; init; }

    public int Skipped { get; init; }

    public int Failed { get; init; }
}
