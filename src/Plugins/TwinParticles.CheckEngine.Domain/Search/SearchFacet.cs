namespace TwinParticles.CheckEngine.Domain.Search;

public sealed class SearchFacet
{
    public string Key { get; init; } = string.Empty;

    public string Value { get; init; } = string.Empty;

    public string? Label { get; init; }

    public int Count { get; init; }
}
