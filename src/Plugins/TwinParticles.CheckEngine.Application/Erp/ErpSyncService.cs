using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TwinParticles.CheckEngine.Domain.Erp;

namespace TwinParticles.CheckEngine.Application.Erp;

public sealed class ErpSyncService
{
    private readonly IErpClientAdapter _clientAdapter;
    private readonly IErpConflictResolutionService _conflictResolutionService;
    private readonly IErpSyncQueueRepository _queueRepository;

    public ErpSyncService(
        IErpSyncQueueRepository queueRepository,
        IErpClientAdapter clientAdapter,
        IErpConflictResolutionService conflictResolutionService)
    {
        _queueRepository = queueRepository;
        _clientAdapter = clientAdapter;
        _conflictResolutionService = conflictResolutionService;
    }

    public async Task<Guid> QueueSyncAsync(ErpSyncEntityType entityType, ErpSyncDirection direction, string localId, string payload, CancellationToken cancellationToken)
    {
        var job = new ErpSyncJob
        {
            JobId = Guid.NewGuid(),
            EntityType = entityType,
            Direction = direction,
            IdempotencyKey = BuildIdempotencyKey(entityType, direction, localId),
            Payload = payload,
            Status = "Queued",
            CreatedUtc = DateTime.UtcNow
        };

        await _queueRepository.EnqueueAsync(job, cancellationToken);
        return job.JobId;
    }

    public async Task<int> ProcessPendingAsync(CancellationToken cancellationToken)
    {
        var pending = await _queueRepository.GetPendingAsync(cancellationToken);
        var successCount = 0;

        foreach (var job in pending)
        {
            job.AttemptCount++;

            var success = await _clientAdapter.PushAsync(job, cancellationToken);
            if (success)
            {
                job.Status = "Succeeded";
                successCount++;
            }
            else
            {
                job.Status = "Failed";
                job.ConflictCode = "erp.push_failed";

                if (_conflictResolutionService.CanAutoResolve(job))
                {
                    _conflictResolutionService.ApplyAutoResolution(job);
                    var retried = await _clientAdapter.PushAsync(job, cancellationToken);
                    job.Status = retried ? "Succeeded" : "Failed";
                    if (retried)
                        successCount++;
                }
            }

            await _queueRepository.UpdateAsync(job, cancellationToken);
        }

        return successCount;
    }

    public async Task<ErpReconciliationReport> BuildReconciliationReportAsync(CancellationToken cancellationToken)
    {
        var all = await _queueRepository.GetAllAsync(cancellationToken);
        var failed = all.Where(x => x.Status == "Failed").ToList();

        return new ErpReconciliationReport
        {
            GeneratedUtc = DateTime.UtcNow,
            TotalJobs = all.Count,
            SuccessfulJobs = all.Count(x => x.Status == "Succeeded"),
            FailedJobs = failed.Count,
            Issues = failed.Select(x => $"{x.EntityType}:{x.ConflictCode ?? "unknown"}").ToList()
        };
    }

    public async Task<string?> PullInventorySnapshotAsync(CancellationToken cancellationToken)
    {
        return await _clientAdapter.PullInventorySnapshotAsync(cancellationToken);
    }

    private static string BuildIdempotencyKey(ErpSyncEntityType entityType, ErpSyncDirection direction, string localId)
    {
        return $"ce:{entityType}:{direction}:{localId.Trim().ToLowerInvariant()}";
    }
}
