using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TwinParticles.CheckEngine.Domain.Erp;

namespace TwinParticles.CheckEngine.Infrastructure.Erp;

public sealed class InMemoryErpSyncQueueRepository : IErpSyncQueueRepository
{
    private readonly List<ErpSyncJob> _jobs = [];

    public Task<Guid> EnqueueAsync(ErpSyncJob job, CancellationToken cancellationToken)
    {
        var existing = _jobs.FirstOrDefault(x => x.IdempotencyKey == job.IdempotencyKey);
        if (existing is not null)
            return Task.FromResult(existing.JobId);

        _jobs.Add(job);
        return Task.FromResult(job.JobId);
    }

    public Task<IReadOnlyList<ErpSyncJob>> GetPendingAsync(CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var staleBefore = now.AddMinutes(-10);
        var items = _jobs
            .Where(x => x.Status == "Queued" && (!x.NextAttemptUtc.HasValue || x.NextAttemptUtc <= now)
                || x.Status == "Processing" && x.LastAttemptUtc < staleBefore)
            .Take(50)
            .ToList();
        foreach (var item in items)
        {
            item.Status = "Processing";
            item.LastAttemptUtc = now;
        }

        return Task.FromResult<IReadOnlyList<ErpSyncJob>>(items);
    }

    public Task<ErpSyncJob?> GetByIdAsync(Guid jobId, CancellationToken cancellationToken)
    {
        return Task.FromResult(_jobs.FirstOrDefault(x => x.JobId == jobId));
    }

    public Task UpdateAsync(ErpSyncJob job, CancellationToken cancellationToken)
    {
        var existing = _jobs.FirstOrDefault(x => x.JobId == job.JobId);
        if (existing is not null)
            _jobs.Remove(existing);

        _jobs.Add(job);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<ErpSyncJob>> GetAllAsync(CancellationToken cancellationToken)
    {
        return Task.FromResult<IReadOnlyList<ErpSyncJob>>(_jobs.ToList());
    }
}
