using System;

namespace TwinParticles.CheckEngine.Domain.Workshop;

/// <summary>
/// Past job reference for a workshop customer vehicle (FR-1011).
/// </summary>
public sealed class WorkshopServiceHistoryEntry
{
    public int JobId { get; set; }

    public WorkshopJobStatus Status { get; set; }

    public decimal LabourEstimate { get; set; }

    public int? OrderId { get; set; }

    public DateTimeOffset UpdatedUtc { get; set; }
}
