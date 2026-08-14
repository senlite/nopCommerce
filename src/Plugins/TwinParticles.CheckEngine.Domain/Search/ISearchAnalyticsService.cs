using System;
using System.Threading;
using System.Threading.Tasks;

namespace TwinParticles.CheckEngine.Domain.Search;

/// <summary>
/// Privacy-safe search analytics (FR-413). Implementations receive normalized query text only to
/// produce a keyed, non-reversible fingerprint; raw text, VIN, OEM, customer, and IP must never be
/// persisted.
/// </summary>
public interface ISearchAnalyticsService
{
    Task<long?> RecordSearchAsync(
        string normalizedQuery,
        SearchMode mode,
        string locale,
        int resultCount,
        bool hasVehicleContext,
        bool widenFitment,
        bool isDegraded,
        long durationMilliseconds,
        CancellationToken cancellationToken);

    Task RecordClickAsync(long analyticsId, int productId, CancellationToken cancellationToken);

    Task<SearchAnalyticsSummary> GetSummaryAsync(DateTime fromUtc, CancellationToken cancellationToken);

    Task<int> PruneAsync(DateTime olderThanUtc, CancellationToken cancellationToken);
}
