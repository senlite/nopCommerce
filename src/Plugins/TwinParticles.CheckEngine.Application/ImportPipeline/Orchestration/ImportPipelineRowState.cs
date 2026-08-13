using System.Collections.Generic;

namespace TwinParticles.CheckEngine.Application.ImportPipeline.Orchestration;

public sealed class ImportPipelineRowState
{
    public int RowNumber { get; set; }

    public IReadOnlyDictionary<string, string?> Fields { get; set; } = new Dictionary<string, string?>();

    public string? OemNumberNormalized { get; set; }

    public int? OemNumberId { get; set; }

    public string? OemErrorCode { get; set; }

    public bool IsDuplicate { get; set; }

    /// <summary>Operator decision for a detected duplicate: None, Pending, Merge, Link, KeepSeparate.</summary>
    public string DuplicateDecision { get; set; } = "None";

    /// <summary>The earlier row this row duplicates, when detected.</summary>
    public int? DuplicateOfRowNumber { get; set; }

    public int? VehicleConfigurationId { get; set; }

    public decimal VehicleMatchConfidence { get; set; }

    public string? Category { get; set; }

    public string? ImageUrl { get; set; }

    public string ReviewStatus { get; set; } = "Pending";

    public string? ReviewReasonCode { get; set; }

    public bool IsPublished { get; set; }

    public string? PublishError { get; set; }

    public HashSet<string> CompletedStages { get; set; } = new(System.StringComparer.Ordinal);

    public string? LastStageError { get; set; }
}
