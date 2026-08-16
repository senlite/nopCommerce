using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LinqToDB.Data;
using Nop.Data;
using TwinParticles.CheckEngine.Domain.Ai;

namespace TwinParticles.CheckEngine.Infrastructure.Ai;

public sealed class SqlAiUsageLedger : IAiUsageLedger
{
    private readonly INopDataProvider _dataProvider;

    public SqlAiUsageLedger(INopDataProvider dataProvider)
    {
        _dataProvider = dataProvider;
    }

    public Task RecordAsync(string featureKey, int tokenUsage, CancellationToken cancellationToken) =>
        RecordOutcomeAsync(featureKey, tokenUsage, success: true, estimatedCostUsd: 0m, cancellationToken);

    public async Task RecordOutcomeAsync(
        string featureKey,
        int tokenUsage,
        bool success,
        decimal estimatedCostUsd,
        CancellationToken cancellationToken)
    {
        var day = DateTime.UtcNow.Date;
        await _dataProvider.ExecuteNonQueryAsync(@"
MERGE TP_CE_AiUsageDaily AS target
USING (SELECT @featureKey AS FeatureKey, @usageDay AS UsageDay) AS source
ON target.FeatureKey = source.FeatureKey AND target.UsageDay = source.UsageDay
WHEN MATCHED THEN UPDATE SET
    TokenUsage = target.TokenUsage + @tokenUsage,
    EstimatedCostUsd = target.EstimatedCostUsd + @estimatedCostUsd,
    AttemptCount = target.AttemptCount + 1,
    FailureCount = target.FailureCount + @failureIncrement
WHEN NOT MATCHED THEN INSERT (FeatureKey, UsageDay, TokenUsage, EstimatedCostUsd, AttemptCount, FailureCount)
VALUES (@featureKey, @usageDay, @tokenUsage, @estimatedCostUsd, 1, @failureIncrement);",
            new DataParameter("featureKey", featureKey),
            new DataParameter("usageDay", day),
            new DataParameter("tokenUsage", Math.Max(0, tokenUsage)),
            new DataParameter("estimatedCostUsd", Math.Max(0m, estimatedCostUsd)),
            new DataParameter("failureIncrement", success ? 0 : 1));
    }

    public async Task<int> GetDailyUsageAsync(string featureKey, CancellationToken cancellationToken)
    {
        var day = DateTime.UtcNow.Date;
        var rows = await _dataProvider.QueryAsync<int>(@"
SELECT TokenUsage FROM TP_CE_AiUsageDaily
WHERE FeatureKey = @featureKey AND UsageDay = @usageDay",
            new DataParameter("featureKey", featureKey),
            new DataParameter("usageDay", day));

        return rows.FirstOrDefault();
    }

    public async Task<int> GetGlobalDailyUsageAsync(CancellationToken cancellationToken)
    {
        var day = DateTime.UtcNow.Date;
        var rows = await _dataProvider.QueryAsync<int>(@"
SELECT COALESCE(SUM(TokenUsage), 0) FROM TP_CE_AiUsageDaily
WHERE UsageDay = @usageDay",
            new DataParameter("usageDay", day));

        return rows.FirstOrDefault();
    }

    public async Task<AiUsageSummary> GetUsageSummaryAsync(string featureKey, CancellationToken cancellationToken)
    {
        var today = DateTime.UtcNow.Date;
        var day7 = today.AddDays(-6);
        var day30 = today.AddDays(-29);

        var rows = await _dataProvider.QueryAsync<UsageAggregateRow>(@"
SELECT
    COALESCE(SUM(CASE WHEN UsageDay = @today THEN TokenUsage ELSE 0 END), 0) AS TodayTokens,
    COALESCE(SUM(CASE WHEN UsageDay >= @day7 THEN TokenUsage ELSE 0 END), 0) AS Last7DaysTokens,
    COALESCE(SUM(CASE WHEN UsageDay >= @day30 THEN TokenUsage ELSE 0 END), 0) AS Last30DaysTokens,
    COALESCE(SUM(CASE WHEN UsageDay = @today THEN AttemptCount ELSE 0 END), 0) AS TodayAttempts,
    COALESCE(SUM(CASE WHEN UsageDay = @today THEN FailureCount ELSE 0 END), 0) AS TodayFailures,
    COALESCE(SUM(CASE WHEN UsageDay >= @day7 THEN AttemptCount ELSE 0 END), 0) AS Last7DaysAttempts,
    COALESCE(SUM(CASE WHEN UsageDay >= @day7 THEN FailureCount ELSE 0 END), 0) AS Last7DaysFailures,
    COALESCE(SUM(CASE WHEN UsageDay >= @day30 THEN AttemptCount ELSE 0 END), 0) AS Last30DaysAttempts,
    COALESCE(SUM(CASE WHEN UsageDay >= @day30 THEN FailureCount ELSE 0 END), 0) AS Last30DaysFailures,
    COALESCE(SUM(CASE WHEN UsageDay = @today THEN EstimatedCostUsd ELSE 0 END), 0) AS TodayEstimatedCostUsd,
    COALESCE(SUM(CASE WHEN UsageDay >= @day7 THEN EstimatedCostUsd ELSE 0 END), 0) AS Last7DaysEstimatedCostUsd,
    COALESCE(SUM(CASE WHEN UsageDay >= @day30 THEN EstimatedCostUsd ELSE 0 END), 0) AS Last30DaysEstimatedCostUsd
FROM TP_CE_AiUsageDaily
WHERE FeatureKey = @featureKey AND UsageDay >= @day30",
            new DataParameter("featureKey", featureKey),
            new DataParameter("today", today),
            new DataParameter("day7", day7),
            new DataParameter("day30", day30));

        var row = rows.FirstOrDefault() ?? new UsageAggregateRow();

        return new AiUsageSummary
        {
            FeatureKey = featureKey,
            TodayTokens = row.TodayTokens,
            Last7DaysTokens = row.Last7DaysTokens,
            Last30DaysTokens = row.Last30DaysTokens,
            TodayAttempts = row.TodayAttempts,
            TodayFailures = row.TodayFailures,
            Last7DaysAttempts = row.Last7DaysAttempts,
            Last7DaysFailures = row.Last7DaysFailures,
            Last30DaysAttempts = row.Last30DaysAttempts,
            Last30DaysFailures = row.Last30DaysFailures,
            TodayEstimatedCostUsd = row.TodayEstimatedCostUsd,
            Last7DaysEstimatedCostUsd = row.Last7DaysEstimatedCostUsd,
            Last30DaysEstimatedCostUsd = row.Last30DaysEstimatedCostUsd
        };
    }

    public async Task<bool> IsCeilingExceededAsync(string featureKey, int ceiling, CancellationToken cancellationToken)
    {
        var usage = await GetDailyUsageAsync(featureKey, cancellationToken);
        return usage >= ceiling;
    }

    private sealed class UsageAggregateRow
    {
        public int TodayTokens { get; init; }
        public int Last7DaysTokens { get; init; }
        public int Last30DaysTokens { get; init; }
        public int TodayAttempts { get; init; }
        public int TodayFailures { get; init; }
        public int Last7DaysAttempts { get; init; }
        public int Last7DaysFailures { get; init; }
        public int Last30DaysAttempts { get; init; }
        public int Last30DaysFailures { get; init; }
        public decimal TodayEstimatedCostUsd { get; init; }
        public decimal Last7DaysEstimatedCostUsd { get; init; }
        public decimal Last30DaysEstimatedCostUsd { get; init; }
    }
}
