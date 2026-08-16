using System.Threading;
using System.Threading.Tasks;

namespace TwinParticles.CheckEngine.Domain.Ai;

public interface IAiUsageLedger
{
    Task RecordAsync(string featureKey, int tokenUsage, CancellationToken cancellationToken);

    Task RecordOutcomeAsync(
        string featureKey,
        int tokenUsage,
        bool success,
        decimal estimatedCostUsd,
        CancellationToken cancellationToken);

    Task<int> GetDailyUsageAsync(string featureKey, CancellationToken cancellationToken);

    Task<int> GetGlobalDailyUsageAsync(CancellationToken cancellationToken);

    Task<AiUsageSummary> GetUsageSummaryAsync(string featureKey, CancellationToken cancellationToken);

    Task<bool> IsCeilingExceededAsync(string featureKey, int ceiling, CancellationToken cancellationToken);
}
