using System;

namespace TwinParticles.CheckEngine.Domain.Erp;

public sealed class ErpSyncJob
{
    public Guid JobId { get; set; }

    public ErpSyncEntityType EntityType { get; set; }

    public ErpSyncDirection Direction { get; set; }

    public string IdempotencyKey { get; set; } = string.Empty;

    public string Payload { get; set; } = string.Empty;

    public int AttemptCount { get; set; }

    public string Status { get; set; } = "Queued";

    public string? ConflictCode { get; set; }

    public DateTime CreatedUtc { get; set; }
}
