using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TwinParticles.CheckEngine.Domain.Erp;

namespace TwinParticles.CheckEngine.Application.Erp;

public sealed class ErpSyncService
{
    private const int MaxAttempts = 5;

    // Money reconciles to the cent; counts must match exactly.
    private const decimal PaymentTolerance = 0.01m;

    private readonly IErpClientAdapter _clientAdapter;
    private readonly IErpConflictResolutionService _conflictResolutionService;
    private readonly IErpSyncQueueRepository _queueRepository;
    private readonly IErpReconciliationDataSource? _reconciliationDataSource;

    public ErpSyncService(
        IErpSyncQueueRepository queueRepository,
        IErpClientAdapter clientAdapter,
        IErpConflictResolutionService conflictResolutionService,
        IErpReconciliationDataSource? reconciliationDataSource = null)
    {
        _queueRepository = queueRepository;
        _clientAdapter = clientAdapter;
        _conflictResolutionService = conflictResolutionService;
        _reconciliationDataSource = reconciliationDataSource;
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

        return await _queueRepository.EnqueueAsync(job, cancellationToken);
    }

    public async Task<int> ProcessPendingAsync(CancellationToken cancellationToken)
    {
        var pending = await _queueRepository.GetPendingAsync(cancellationToken);
        var successCount = 0;

        foreach (var job in pending)
        {
            job.AttemptCount++;
            job.LastAttemptUtc = DateTime.UtcNow;

            var success = job.Direction == ErpSyncDirection.PullFromErp
                ? await TryPullAsync(job, cancellationToken)
                : await TryPushAsync(job, cancellationToken);
            if (success)
            {
                job.Status = "Succeeded";
                job.ConflictCode = null;
                job.NextAttemptUtc = null;
                successCount++;
            }
            else
            {
                job.ConflictCode = job.Direction == ErpSyncDirection.PullFromErp
                    ? "erp.pull_failed"
                    : "erp.push_failed";

                if (_conflictResolutionService.CanAutoResolve(job))
                {
                    _conflictResolutionService.ApplyAutoResolution(job);
                    var retried = job.Direction == ErpSyncDirection.PullFromErp
                        ? await TryPullAsync(job, cancellationToken)
                        : await TryPushAsync(job, cancellationToken);
                    if (retried)
                    {
                        job.Status = "Succeeded";
                        job.ConflictCode = null;
                        job.NextAttemptUtc = null;
                        successCount++;
                    }
                }

                if (job.Status != "Succeeded")
                    ScheduleRetryOrFail(job);
            }

            await _queueRepository.UpdateAsync(job, cancellationToken);
        }

        return successCount;
    }

    public Task<ErpReconciliationReport> BuildReconciliationReportAsync(CancellationToken cancellationToken)
        => BuildReconciliationReportAsync(null, null, cancellationToken);

    /// <summary>
    /// Builds the queue summary and, when a data source is configured and both sides are available,
    /// compares order count, payment total and inventory units across systems for the given window
    /// (default: the trailing 24 hours) per FR-825.
    /// </summary>
    public async Task<ErpReconciliationReport> BuildReconciliationReportAsync(
        DateTime? windowFromUtc,
        DateTime? windowToUtc,
        CancellationToken cancellationToken)
    {
        var all = await _queueRepository.GetAllAsync(cancellationToken);
        var failed = all.Where(x => x.Status == "Failed").ToList();
        var issues = failed.Select(x => $"{x.EntityType}:{x.ConflictCode ?? "unknown"}").ToList();

        var toUtc = windowToUtc ?? DateTime.UtcNow;
        var fromUtc = windowFromUtc ?? toUtc.AddDays(-1);

        var variances = new List<ErpReconciliationVariance>();
        var hasDiscrepancy = false;

        if (_reconciliationDataSource is not null)
        {
            var local = await _reconciliationDataSource.GetLocalTotalsAsync(fromUtc, toUtc, cancellationToken);
            var erp = await _reconciliationDataSource.GetErpTotalsAsync(fromUtc, toUtc, cancellationToken);

            if (!local.IsAvailable)
                issues.Add("reconciliation:local_unavailable");
            if (!erp.IsAvailable)
                issues.Add("reconciliation:erp_unavailable");

            if (local.IsAvailable && erp.IsAvailable)
            {
                variances.Add(BuildVariance("order.count", local.OrderCount, erp.OrderCount, 0m));
                variances.Add(BuildVariance("payment.total", local.PaymentTotal, erp.PaymentTotal, PaymentTolerance));
                variances.Add(BuildVariance("inventory.units", local.InventoryUnits, erp.InventoryUnits, 0m));
                hasDiscrepancy = variances.Any(v => !v.WithinTolerance);
            }
        }

        return new ErpReconciliationReport
        {
            GeneratedUtc = DateTime.UtcNow,
            TotalJobs = all.Count,
            SuccessfulJobs = all.Count(x => x.Status == "Succeeded"),
            FailedJobs = failed.Count,
            Issues = issues,
            Variances = variances,
            HasFinancialDiscrepancy = hasDiscrepancy,
            WindowFromUtc = variances.Count > 0 ? fromUtc : null,
            WindowToUtc = variances.Count > 0 ? toUtc : null
        };
    }

    private static ErpReconciliationVariance BuildVariance(string metric, decimal local, decimal erp, decimal tolerance)
    {
        var absolute = Math.Abs(local - erp);
        return new ErpReconciliationVariance
        {
            Metric = metric,
            Local = local,
            Erp = erp,
            AbsoluteVariance = absolute,
            WithinTolerance = absolute <= tolerance
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

    private async Task<bool> TryPushAsync(ErpSyncJob job, CancellationToken cancellationToken)
    {
        try
        {
            return await _clientAdapter.PushAsync(job, cancellationToken);
        }
        catch when (!cancellationToken.IsCancellationRequested)
        {
            return false;
        }
    }

    private async Task<bool> TryPullAsync(ErpSyncJob job, CancellationToken cancellationToken)
    {
        try
        {
            return await _clientAdapter.PullAsync(job, cancellationToken);
        }
        catch when (!cancellationToken.IsCancellationRequested)
        {
            return false;
        }
    }

    private static void ScheduleRetryOrFail(ErpSyncJob job)
    {
        if (job.AttemptCount >= MaxAttempts)
        {
            job.Status = "Failed";
            job.NextAttemptUtc = null;
            return;
        }

        // 2, 4, 8, 16 minute backoff. The schedule task may run more frequently, but the SQL claim
        // excludes jobs until this timestamp and recovers processing claims stale for ten minutes.
        var delayMinutes = Math.Min(1 << job.AttemptCount, 60);
        job.Status = "Queued";
        job.NextAttemptUtc = DateTime.UtcNow.AddMinutes(delayMinutes);
        job.ConflictCode = "erp.retry_scheduled";
    }
}
