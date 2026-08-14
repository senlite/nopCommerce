using System.Threading;
using System.Threading.Tasks;

namespace TwinParticles.CheckEngine.Domain.Search;

public interface ISearchIndexHealthService
{
    Task<bool> IsHealthyAsync(CancellationToken cancellationToken);

    Task ReportDegradedAsync(string reason, CancellationToken cancellationToken);

    Task RebuildAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Refreshes only the entries changed since the last projection cursor. Implementations that do
    /// not maintain an incremental cursor safely fall back to a full rebuild.
    /// </summary>
    Task RefreshIncrementalAsync(CancellationToken cancellationToken) => RebuildAsync(cancellationToken);
}
