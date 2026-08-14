using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LinqToDB.Data;
using Nop.Data;
using TwinParticles.CheckEngine.Domain.Search;

namespace TwinParticles.CheckEngine.Infrastructure.Search;

public sealed class SqlSearchAnalyticsService : ISearchAnalyticsService
{
    private readonly INopDataProvider _dataProvider;
    private readonly ISearchQueryFingerprintService _fingerprintService;

    public SqlSearchAnalyticsService(
        INopDataProvider dataProvider,
        ISearchQueryFingerprintService fingerprintService)
    {
        _dataProvider = dataProvider;
        _fingerprintService = fingerprintService;
    }

    public async Task<long?> RecordSearchAsync(
        string normalizedQuery,
        SearchMode mode,
        string locale,
        int resultCount,
        bool hasVehicleContext,
        bool widenFitment,
        bool isDegraded,
        long durationMilliseconds,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var rows = await _dataProvider.QueryAsync<ScalarLongRow>(@"
INSERT INTO TP_CE_SearchAnalytics
(QueryFingerprint, ModeId, Locale, ResultCount, HasVehicleContext, WidenFitment, IsDegraded, DurationBucketMs, CreatedUtc, ClickedProductId, ClickedUtc)
VALUES
(@queryFingerprint, @modeId, @locale, @resultCount, @hasVehicleContext, @widenFitment, @isDegraded, @durationBucketMs, @createdUtc, NULL, NULL);
SELECT CAST(SCOPE_IDENTITY() AS bigint) AS Value;",
            new DataParameter("queryFingerprint", _fingerprintService.Create(normalizedQuery)),
            new DataParameter("modeId", (int)mode),
            new DataParameter("locale", NormalizeLocale(locale)),
            new DataParameter("resultCount", Math.Max(0, resultCount)),
            new DataParameter("hasVehicleContext", hasVehicleContext),
            new DataParameter("widenFitment", widenFitment),
            new DataParameter("isDegraded", isDegraded),
            new DataParameter("durationBucketMs", BucketDuration(durationMilliseconds)),
            new DataParameter("createdUtc", DateTime.UtcNow));

        return rows.Single().Value;
    }

    public Task RecordClickAsync(long analyticsId, int productId, CancellationToken cancellationToken)
    {
        if (analyticsId <= 0 || productId <= 0)
            return Task.CompletedTask;

        return _dataProvider.ExecuteNonQueryAsync(@"
UPDATE TP_CE_SearchAnalytics
SET ClickedProductId=@productId, ClickedUtc=@clickedUtc
WHERE Id=@id AND ClickedUtc IS NULL",
            new DataParameter("productId", productId),
            new DataParameter("clickedUtc", DateTime.UtcNow),
            new DataParameter("id", analyticsId));
    }

    public async Task<SearchAnalyticsSummary> GetSummaryAsync(
        DateTime fromUtc,
        CancellationToken cancellationToken)
    {
        var rows = await _dataProvider.QueryAsync<SummaryRow>(@"
SELECT COUNT(*) AS SearchCount,
       COALESCE(SUM(CASE WHEN ResultCount=0 THEN 1 ELSE 0 END), 0) AS ZeroResultCount,
       COALESCE(SUM(CASE WHEN ClickedUtc IS NOT NULL THEN 1 ELSE 0 END), 0) AS ClickCount
FROM TP_CE_SearchAnalytics
WHERE CreatedUtc>=@fromUtc",
            new DataParameter("fromUtc", fromUtc));
        var row = rows.Single();
        return new SearchAnalyticsSummary
        {
            FromUtc = fromUtc,
            SearchCount = row.SearchCount,
            ZeroResultCount = row.ZeroResultCount,
            ClickCount = row.ClickCount,
            ClickThroughRate = row.SearchCount == 0
                ? 0
                : decimal.Round((decimal)row.ClickCount / row.SearchCount, 4)
        };
    }

    public Task<int> PruneAsync(DateTime olderThanUtc, CancellationToken cancellationToken)
        => _dataProvider.ExecuteNonQueryAsync(
            "DELETE FROM TP_CE_SearchAnalytics WHERE CreatedUtc<@olderThanUtc",
            new DataParameter("olderThanUtc", olderThanUtc));

    private static string NormalizeLocale(string locale)
        => string.IsNullOrWhiteSpace(locale)
            ? "en"
            : locale.Trim().ToLowerInvariant()[..Math.Min(locale.Trim().Length, 16)];

    private static int BucketDuration(long milliseconds)
    {
        int[] buckets = [25, 50, 100, 200, 300, 600, 1000, 2000];
        foreach (var bucket in buckets)
        {
            if (milliseconds <= bucket)
                return bucket;
        }

        return 5000;
    }

    private sealed class ScalarLongRow
    {
        public long Value { get; set; }
    }

    private sealed class SummaryRow
    {
        public int SearchCount { get; set; }
        public int ZeroResultCount { get; set; }
        public int ClickCount { get; set; }
    }
}
