using System;

namespace TwinParticles.CheckEngine.Domain.Search;

/// <summary>Durable state of the keyword search projection.</summary>
public sealed class SearchIndexState
{
    public bool IsHealthy { get; init; } = true;

    public DateTime? LastRebuildUtc { get; init; }

    public DateTime? LastCursorUtc { get; init; }

    public int IndexedCount { get; init; }

    /// <summary>True once a full rebuild has materialized the projection at least once.</summary>
    public bool IsReady => LastRebuildUtc.HasValue && IsHealthy && IndexedCount > 0;
}
