using System.Collections.Generic;

namespace TwinParticles.CheckEngine.Domain.Search;

public sealed class SearchResult
{
    public SearchMode ModeUsed { get; init; }

    public IReadOnlyList<SearchHit> Hits { get; init; } = [];

    public int Total { get; init; }

    public IReadOnlyList<SearchFacet> Facets { get; init; } = [];

    public IReadOnlyList<string> Suggestions { get; init; } = [];

    public IReadOnlyList<SearchRecoveryAction> Recovery { get; init; } = [];

    public bool IsDegraded { get; init; }

    public long? AnalyticsId { get; init; }

    public bool NeedsDisambiguation { get; init; }

    public IReadOnlyList<SearchVinCandidate> VinCandidates { get; init; } = [];

    /// <summary>Structured NL parse output when <see cref="ModeUsed"/> is natural language.</summary>
    public SearchParsedIntent? ParsedIntent { get; init; }
}
