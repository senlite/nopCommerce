using System.Threading;
using System.Threading.Tasks;
using TwinParticles.CheckEngine.Domain.Erp;

namespace TwinParticles.CheckEngine.Infrastructure.Erp;

public sealed class StubErpClientAdapter : IErpClientAdapter
{
    public Task<bool> PushAsync(ErpSyncJob job, CancellationToken cancellationToken)
    {
        var success = !job.Payload.Contains("force-fail");
        return Task.FromResult(success);
    }

    public Task<bool> PullAsync(ErpSyncJob job, CancellationToken cancellationToken)
        => PushAsync(job, cancellationToken);

    public Task<string?> PullInventorySnapshotAsync(CancellationToken cancellationToken)
    {
        return Task.FromResult<string?>("{\"warehouse\":\"MAIN\",\"items\":[]}");
    }
}
