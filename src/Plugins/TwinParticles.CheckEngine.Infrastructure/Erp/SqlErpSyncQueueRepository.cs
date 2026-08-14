using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LinqToDB.Data;
using Nop.Data;
using TwinParticles.CheckEngine.Domain.Erp;
using TwinParticles.CheckEngine.Infrastructure.Data;

namespace TwinParticles.CheckEngine.Infrastructure.Erp;

public sealed class SqlErpSyncQueueRepository : IErpSyncQueueRepository
{
    private readonly INopDataProvider _dataProvider;

    public SqlErpSyncQueueRepository(INopDataProvider dataProvider)
    {
        _dataProvider = dataProvider;
    }

    public async Task<Guid> EnqueueAsync(ErpSyncJob job, CancellationToken cancellationToken)
    {
        if (job.JobId == Guid.Empty)
            job.JobId = Guid.NewGuid();

        if (job.CreatedUtc == default)
            job.CreatedUtc = DateTime.UtcNow;

        var existing = await _dataProvider.QueryAsync<JobIdRow>(
            "SELECT JobId FROM TP_CE_ErpSyncJob WHERE IdempotencyKey = @idempotencyKey",
            new DataParameter("idempotencyKey", job.IdempotencyKey));
        var existingId = existing.FirstOrDefault()?.JobId;
        if (existingId is { } matched && matched != Guid.Empty)
            return matched;

        try
        {
            await _dataProvider.ExecuteNonQueryAsync(
                @"INSERT INTO TP_CE_ErpSyncJob
(JobId, EntityType, Direction, IdempotencyKey, Payload, AttemptCount, Status, ConflictCode, CreatedUtc, LastAttemptUtc, NextAttemptUtc)
VALUES
(@jobId, @entityType, @direction, @idempotencyKey, @payload, @attemptCount, @status, @conflictCode, @createdUtc, NULL, NULL)",
                new DataParameter("jobId", job.JobId),
                new DataParameter("entityType", (int)job.EntityType),
                new DataParameter("direction", (int)job.Direction),
                new DataParameter("idempotencyKey", job.IdempotencyKey),
                new DataParameter("payload", job.Payload),
                new DataParameter("attemptCount", job.AttemptCount),
                new DataParameter("status", job.Status),
                new DataParameter("conflictCode", job.ConflictCode),
                new DataParameter("createdUtc", job.CreatedUtc));

            return job.JobId;
        }
        catch
        {
            var raced = await _dataProvider.QueryAsync<JobIdRow>(
                "SELECT JobId FROM TP_CE_ErpSyncJob WHERE IdempotencyKey = @idempotencyKey",
                new DataParameter("idempotencyKey", job.IdempotencyKey));
            var racedId = raced.FirstOrDefault()?.JobId;
            if (racedId is { } recovered && recovered != Guid.Empty)
                return recovered;

            throw;
        }
    }

    public async Task<IReadOnlyList<ErpSyncJob>> GetPendingAsync(CancellationToken cancellationToken)
    {
        var staleBefore = DateTime.UtcNow.AddMinutes(-10);
        var rows = await _dataProvider.QueryAsync<ErpSyncJobRow>(
            CheckEngineSql.SelectTop(
                50,
                "JobId, EntityType, Direction, IdempotencyKey, Payload, AttemptCount, Status, ConflictCode, CreatedUtc, LastAttemptUtc, NextAttemptUtc",
                @"FROM TP_CE_ErpSyncJob
WHERE (Status = 'Queued' AND (NextAttemptUtc IS NULL OR NextAttemptUtc <= @now))
   OR (Status = 'Processing' AND LastAttemptUtc < @staleBefore)
ORDER BY CreatedUtc, Id"),
            new DataParameter("now", DateTime.UtcNow),
            new DataParameter("staleBefore", staleBefore));

        var claimed = new List<ErpSyncJob>();
        foreach (var row in rows)
        {
            await _dataProvider.ExecuteNonQueryAsync(
                @"UPDATE TP_CE_ErpSyncJob
SET Status = 'Processing', LastAttemptUtc = @now
WHERE JobId = @jobId",
                new DataParameter("now", DateTime.UtcNow),
                new DataParameter("jobId", row.JobId));

            row.Status = "Processing";
            row.LastAttemptUtc = DateTime.UtcNow;
            claimed.Add(Map(row));
        }

        return claimed;
    }

    public async Task<ErpSyncJob?> GetByIdAsync(Guid jobId, CancellationToken cancellationToken)
    {
        var rows = await _dataProvider.QueryAsync<ErpSyncJobRow>(
            @"SELECT JobId, EntityType, Direction, IdempotencyKey, Payload, AttemptCount, Status, ConflictCode, CreatedUtc, LastAttemptUtc, NextAttemptUtc
FROM TP_CE_ErpSyncJob
WHERE JobId = @jobId",
            new DataParameter("jobId", jobId));

        return rows.Select(Map).FirstOrDefault();
    }

    public async Task UpdateAsync(ErpSyncJob job, CancellationToken cancellationToken)
    {
        await _dataProvider.ExecuteNonQueryAsync(
            @"UPDATE TP_CE_ErpSyncJob
SET EntityType = @entityType,
    Direction = @direction,
    IdempotencyKey = @idempotencyKey,
    Payload = @payload,
    AttemptCount = @attemptCount,
    Status = @status,
    ConflictCode = @conflictCode,
    LastAttemptUtc = @lastAttemptUtc,
    NextAttemptUtc = @nextAttemptUtc
WHERE JobId = @jobId",
            new DataParameter("entityType", (int)job.EntityType),
            new DataParameter("direction", (int)job.Direction),
            new DataParameter("idempotencyKey", job.IdempotencyKey),
            new DataParameter("payload", job.Payload),
            new DataParameter("attemptCount", job.AttemptCount),
            new DataParameter("status", job.Status),
            new DataParameter("conflictCode", job.ConflictCode),
            new DataParameter("lastAttemptUtc", job.LastAttemptUtc),
            new DataParameter("nextAttemptUtc", job.NextAttemptUtc),
            new DataParameter("jobId", job.JobId));
    }

    public async Task<IReadOnlyList<ErpSyncJob>> GetAllAsync(CancellationToken cancellationToken)
    {
        var rows = await _dataProvider.QueryAsync<ErpSyncJobRow>(
            @"SELECT JobId, EntityType, Direction, IdempotencyKey, Payload, AttemptCount, Status, ConflictCode, CreatedUtc, LastAttemptUtc, NextAttemptUtc
FROM TP_CE_ErpSyncJob
ORDER BY CreatedUtc, Id");

        return rows.Select(Map).ToList();
    }

    private static ErpSyncJob Map(ErpSyncJobRow row)
        => new()
        {
            JobId = row.JobId,
            EntityType = (ErpSyncEntityType)row.EntityType,
            Direction = (ErpSyncDirection)row.Direction,
            IdempotencyKey = row.IdempotencyKey,
            Payload = row.Payload,
            AttemptCount = row.AttemptCount,
            Status = row.Status,
            ConflictCode = row.ConflictCode,
            CreatedUtc = row.CreatedUtc,
            LastAttemptUtc = row.LastAttemptUtc,
            NextAttemptUtc = row.NextAttemptUtc
        };

    private sealed class JobIdRow
    {
        public Guid JobId { get; set; }
    }

    private sealed class ErpSyncJobRow
    {
        public Guid JobId { get; set; }
        public int EntityType { get; set; }
        public int Direction { get; set; }
        public string IdempotencyKey { get; set; } = string.Empty;
        public string Payload { get; set; } = string.Empty;
        public int AttemptCount { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? ConflictCode { get; set; }
        public DateTime CreatedUtc { get; set; }
        public DateTime? LastAttemptUtc { get; set; }
        public DateTime? NextAttemptUtc { get; set; }
    }
}
