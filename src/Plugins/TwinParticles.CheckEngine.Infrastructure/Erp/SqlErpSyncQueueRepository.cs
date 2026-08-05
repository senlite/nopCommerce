using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LinqToDB.Data;
using Nop.Data;
using TwinParticles.CheckEngine.Domain.Erp;

namespace TwinParticles.CheckEngine.Infrastructure.Erp;

public sealed class SqlErpSyncQueueRepository : IErpSyncQueueRepository
{
    private readonly INopDataProvider _dataProvider;

    public SqlErpSyncQueueRepository(INopDataProvider dataProvider)
    {
        _dataProvider = dataProvider;
    }

    public async Task EnqueueAsync(ErpSyncJob job, CancellationToken cancellationToken)
    {
        if (job.JobId == Guid.Empty)
            job.JobId = Guid.NewGuid();

        if (job.CreatedUtc == default)
            job.CreatedUtc = DateTime.UtcNow;

        await _dataProvider.ExecuteNonQueryAsync(
            @"INSERT INTO TP_CE_ErpSyncJob
(JobId, EntityType, Direction, IdempotencyKey, Payload, AttemptCount, Status, ConflictCode, CreatedUtc)
VALUES
(@jobId, @entityType, @direction, @idempotencyKey, @payload, @attemptCount, @status, @conflictCode, @createdUtc)",
            new DataParameter("jobId", job.JobId),
            new DataParameter("entityType", (int)job.EntityType),
            new DataParameter("direction", (int)job.Direction),
            new DataParameter("idempotencyKey", job.IdempotencyKey),
            new DataParameter("payload", job.Payload),
            new DataParameter("attemptCount", job.AttemptCount),
            new DataParameter("status", job.Status),
            new DataParameter("conflictCode", job.ConflictCode),
            new DataParameter("createdUtc", job.CreatedUtc));
    }

    public async Task<IReadOnlyList<ErpSyncJob>> GetPendingAsync(CancellationToken cancellationToken)
    {
        var rows = await _dataProvider.QueryAsync<ErpSyncJobRow>(
            @"SELECT JobId, EntityType, Direction, IdempotencyKey, Payload, AttemptCount, Status, ConflictCode, CreatedUtc
FROM TP_CE_ErpSyncJob
WHERE Status = @status
ORDER BY CreatedUtc, Id",
            new DataParameter("status", "Queued"));

        return rows.Select(Map).ToList();
    }

    public async Task<ErpSyncJob?> GetByIdAsync(Guid jobId, CancellationToken cancellationToken)
    {
        var rows = await _dataProvider.QueryAsync<ErpSyncJobRow>(
            @"SELECT JobId, EntityType, Direction, IdempotencyKey, Payload, AttemptCount, Status, ConflictCode, CreatedUtc
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
    ConflictCode = @conflictCode
WHERE JobId = @jobId",
            new DataParameter("entityType", (int)job.EntityType),
            new DataParameter("direction", (int)job.Direction),
            new DataParameter("idempotencyKey", job.IdempotencyKey),
            new DataParameter("payload", job.Payload),
            new DataParameter("attemptCount", job.AttemptCount),
            new DataParameter("status", job.Status),
            new DataParameter("conflictCode", job.ConflictCode),
            new DataParameter("jobId", job.JobId));
    }

    public async Task<IReadOnlyList<ErpSyncJob>> GetAllAsync(CancellationToken cancellationToken)
    {
        var rows = await _dataProvider.QueryAsync<ErpSyncJobRow>(
            @"SELECT JobId, EntityType, Direction, IdempotencyKey, Payload, AttemptCount, Status, ConflictCode, CreatedUtc
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
            CreatedUtc = row.CreatedUtc
        };

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
    }
}
