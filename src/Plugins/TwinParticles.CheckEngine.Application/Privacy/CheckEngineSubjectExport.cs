using System;
using System.Collections.Generic;
using TwinParticles.CheckEngine.Application.Garage;

namespace TwinParticles.CheckEngine.Application.Privacy;

/// <summary>
/// A data subject's complete Check Engine export (FR-960), aggregating every Check Engine-owned
/// personal-data domain plus an explicit statement of what is retained under a legal hold (FR-961).
/// </summary>
public sealed class CheckEngineSubjectExport
{
    public int CustomerId { get; init; }

    public DateTime ExportedUtc { get; init; }

    public required GaragePrivacyExport Garage { get; init; }

    /// <summary>Data classes that are deliberately retained on erasure, with the governing reason.</summary>
    public IReadOnlyList<RetentionHold> RetentionHolds { get; init; } = [];
}

public sealed class RetentionHold
{
    public required string DataClass { get; init; }

    public required string Reason { get; init; }

    public required string Disposition { get; init; }
}
