using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace TwinParticles.CheckEngine.Domain.Erp;

public interface IErpSyncQueueRepository
{
    Task EnqueueAsync(ErpSyncJob job, CancellationToken cancellationToken);

    Task<IReadOnlyList<ErpSyncJob>> GetPendingAsync(CancellationToken cancellationToken);

    Task<ErpSyncJob?> GetByIdAsync(Guid jobId, CancellationToken cancellationToken);

    Task UpdateAsync(ErpSyncJob job, CancellationToken cancellationToken);

    Task<IReadOnlyList<ErpSyncJob>> GetAllAsync(CancellationToken cancellationToken);
}
