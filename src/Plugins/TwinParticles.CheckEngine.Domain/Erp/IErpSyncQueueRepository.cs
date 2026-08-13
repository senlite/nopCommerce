using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace TwinParticles.CheckEngine.Domain.Erp;

public interface IErpSyncQueueRepository
{
    /// <summary>
    /// Idempotently enqueues a job and returns the durable job id. Repeated idempotency keys return
    /// the existing job instead of throwing or creating duplicate work.
    /// </summary>
    Task<Guid> EnqueueAsync(ErpSyncJob job, CancellationToken cancellationToken);

    /// <summary>
    /// Atomically claims eligible queued jobs. Implementations also recover stale processing claims.
    /// </summary>
    Task<IReadOnlyList<ErpSyncJob>> GetPendingAsync(CancellationToken cancellationToken);

    Task<ErpSyncJob?> GetByIdAsync(Guid jobId, CancellationToken cancellationToken);

    Task UpdateAsync(ErpSyncJob job, CancellationToken cancellationToken);

    Task<IReadOnlyList<ErpSyncJob>> GetAllAsync(CancellationToken cancellationToken);
}
