using System;

namespace TwinParticles.CheckEngine.Domain.Search;

public sealed class SearchAnalyticsSummary
{
    public DateTime FromUtc { get; init; }
    public int SearchCount { get; init; }
    public int ZeroResultCount { get; init; }
    public int ClickCount { get; init; }
    public decimal ClickThroughRate { get; init; }
}
