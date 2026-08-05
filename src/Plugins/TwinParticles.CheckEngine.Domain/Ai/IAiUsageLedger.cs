using System.Threading;
using System.Threading.Tasks;

namespace TwinParticles.CheckEngine.Domain.Ai;

public interface IAiUsageLedger
{
    Task RecordAsync(string featureKey, int tokenUsage, CancellationToken cancellationToken);

    Task<int> GetDailyUsageAsync(string featureKey, CancellationToken cancellationToken);

    Task<bool> IsCeilingExceededAsync(string featureKey, int ceiling, CancellationToken cancellationToken);
}
