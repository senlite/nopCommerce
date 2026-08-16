using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TwinParticles.CheckEngine.Domain.Ai;

namespace TwinParticles.CheckEngine.Infrastructure.Ai;

public sealed class InMemoryAiUsageLedger : IAiUsageLedger
{
    private readonly ConcurrentDictionary<string, DailyUsageStats> _usageByFeatureDay = new(StringComparer.OrdinalIgnoreCase);

    public Task RecordAsync(string featureKey, int tokenUsage, CancellationToken cancellationToken) =>
        RecordOutcomeAsync(featureKey, tokenUsage, success: true, estimatedCostUsd: 0m, cancellationToken);

    public Task RecordOutcomeAsync(
        string featureKey,
        int tokenUsage,
        bool success,
        decimal estimatedCostUsd,
        CancellationToken cancellationToken)
    {
        var key = BuildKey(featureKey);
        _usageByFeatureDay.AddOrUpdate(
            key,
            _ => new DailyUsageStats(Math.Max(0, tokenUsage), Math.Max(0m, estimatedCostUsd), 1, success ? 0 : 1),
            (_, existing) => existing.Add(Math.Max(0, tokenUsage), Math.Max(0m, estimatedCostUsd), success));

        return Task.CompletedTask;
    }

    public Task<int> GetDailyUsageAsync(string featureKey, CancellationToken cancellationToken)
    {
        var key = BuildKey(featureKey);
        return Task.FromResult(_usageByFeatureDay.TryGetValue(key, out var usage) ? usage.TokenUsage : 0);
    }

    public Task<int> GetGlobalDailyUsageAsync(CancellationToken cancellationToken)
    {
        var dayPrefix = $"{DateTime.UtcNow:yyyy-MM-dd}:";
        var total = _usageByFeatureDay
            .Where(pair => pair.Key.StartsWith(dayPrefix, StringComparison.OrdinalIgnoreCase))
            .Sum(pair => pair.Value.TokenUsage);

        return Task.FromResult(total);
    }

    public Task<AiUsageSummary> GetUsageSummaryAsync(string featureKey, CancellationToken cancellationToken)
    {
        var today = DateTime.UtcNow.Date;
        var day7 = today.AddDays(-6);
        var day30 = today.AddDays(-29);

        var rows = _usageByFeatureDay
            .Where(pair => pair.Key.EndsWith($":{featureKey}", StringComparison.OrdinalIgnoreCase))
            .Select(pair => ParseEntry(pair.Key, pair.Value))
            .Where(entry => entry.Day >= day30)
            .ToList();

        return Task.FromResult(new AiUsageSummary
        {
            FeatureKey = featureKey,
            TodayTokens = SumTokens(rows, today, today),
            Last7DaysTokens = SumTokens(rows, day7, today),
            Last30DaysTokens = SumTokens(rows, day30, today),
            TodayAttempts = SumAttempts(rows, today, today),
            TodayFailures = SumFailures(rows, today, today),
            Last7DaysAttempts = SumAttempts(rows, day7, today),
            Last7DaysFailures = SumFailures(rows, day7, today),
            Last30DaysAttempts = SumAttempts(rows, day30, today),
            Last30DaysFailures = SumFailures(rows, day30, today),
            TodayEstimatedCostUsd = SumCost(rows, today, today),
            Last7DaysEstimatedCostUsd = SumCost(rows, day7, today),
            Last30DaysEstimatedCostUsd = SumCost(rows, day30, today)
        });
    }

    public async Task<bool> IsCeilingExceededAsync(string featureKey, int ceiling, CancellationToken cancellationToken)
    {
        var usage = await GetDailyUsageAsync(featureKey, cancellationToken);
        return usage >= ceiling;
    }

    private static string BuildKey(string featureKey) =>
        $"{DateTime.UtcNow:yyyy-MM-dd}:{featureKey}";

    private static (DateTime Day, DailyUsageStats Stats) ParseEntry(string key, DailyUsageStats stats)
    {
        var separator = key.IndexOf(':');
        var dayText = separator > 0 ? key[..separator] : key;
        return DateTime.TryParse(dayText, out var day)
            ? (day.Date, stats)
            : (DateTime.UtcNow.Date, stats);
    }

    private static int SumTokens(System.Collections.Generic.IReadOnlyList<(DateTime Day, DailyUsageStats Stats)> rows, DateTime from, DateTime to) =>
        rows.Where(row => row.Day >= from && row.Day <= to).Sum(row => row.Stats.TokenUsage);

    private static decimal SumCost(System.Collections.Generic.IReadOnlyList<(DateTime Day, DailyUsageStats Stats)> rows, DateTime from, DateTime to) =>
        rows.Where(row => row.Day >= from && row.Day <= to).Sum(row => row.Stats.EstimatedCostUsd);

    private static int SumAttempts(System.Collections.Generic.IReadOnlyList<(DateTime Day, DailyUsageStats Stats)> rows, DateTime from, DateTime to) =>
        rows.Where(row => row.Day >= from && row.Day <= to).Sum(row => row.Stats.AttemptCount);

    private static int SumFailures(System.Collections.Generic.IReadOnlyList<(DateTime Day, DailyUsageStats Stats)> rows, DateTime from, DateTime to) =>
        rows.Where(row => row.Day >= from && row.Day <= to).Sum(row => row.Stats.FailureCount);

    private sealed class DailyUsageStats
    {
        public DailyUsageStats(int tokenUsage, decimal estimatedCostUsd, int attemptCount, int failureCount)
        {
            TokenUsage = tokenUsage;
            EstimatedCostUsd = estimatedCostUsd;
            AttemptCount = attemptCount;
            FailureCount = failureCount;
        }

        public int TokenUsage { get; private set; }
        public decimal EstimatedCostUsd { get; private set; }
        public int AttemptCount { get; private set; }
        public int FailureCount { get; private set; }

        public DailyUsageStats Add(int tokenUsage, decimal estimatedCostUsd, bool success)
        {
            TokenUsage += tokenUsage;
            EstimatedCostUsd += estimatedCostUsd;
            AttemptCount++;
            if (!success)
                FailureCount++;

            return this;
        }
    }
}
