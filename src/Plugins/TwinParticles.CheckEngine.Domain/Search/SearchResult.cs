using System.Collections.Generic;

namespace TwinParticles.CheckEngine.Domain.Search;

public sealed class SearchResult
{
    public SearchMode ModeUsed { get; init; }

    public IReadOnlyList<SearchHit> Hits { get; init; } = [];

    public IReadOnlyList<SearchFacet> Facets { get; init; } = [];

    public IReadOnlyList<string> Suggestions { get; init; } = [];

    public bool IsDegraded { get; init; }
}
