using System.Threading;
using System.Threading.Tasks;

namespace TwinParticles.CheckEngine.Domain.Erp;

public interface IErpClientAdapter
{
    Task<bool> PushAsync(ErpSyncJob job, CancellationToken cancellationToken);

    Task<bool> PullAsync(ErpSyncJob job, CancellationToken cancellationToken);

    Task<string?> PullInventorySnapshotAsync(CancellationToken cancellationToken);
}
