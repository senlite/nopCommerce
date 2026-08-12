using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using TwinParticles.CheckEngine.Domain.Ai;

namespace TwinParticles.CheckEngine.Infrastructure.Ai;

public sealed class InMemoryAiUsageLedger : IAiUsageLedger
{
    private readonly ConcurrentDictionary<string, int> _usageByFeatureDay = new(StringComparer.OrdinalIgnoreCase);

    public Task RecordAsync(string featureKey, int tokenUsage, CancellationToken cancellationToken)
    {
        var key = BuildKey(featureKey);
        _usageByFeatureDay.AddOrUpdate(key, Math.Max(0, tokenUsage), (_, existing) => existing + Math.Max(0, tokenUsage));
        return Task.CompletedTask;
    }

    public Task<int> GetDailyUsageAsync(string featureKey, CancellationToken cancellationToken)
    {
        var key = BuildKey(featureKey);
        return Task.FromResult(_usageByFeatureDay.TryGetValue(key, out var usage) ? usage : 0);
    }

    public async Task<bool> IsCeilingExceededAsync(string featureKey, int ceiling, CancellationToken cancellationToken)
    {
        var usage = await GetDailyUsageAsync(featureKey, cancellationToken);
        return usage >= ceiling;
    }

    private static string BuildKey(string featureKey)
    {
        return $"{DateTime.UtcNow:yyyy-MM-dd}:{featureKey}";
    }
}
