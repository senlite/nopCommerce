using System.Collections.Generic;

namespace TwinParticles.CheckEngine.Application.ImportPipeline.Orchestration;

public sealed class ImportPipelineRowState
{
    public int RowNumber { get; init; }

    public IReadOnlyDictionary<string, string?> Fields { get; set; } = new Dictionary<string, string?>();

    public string? OemNumberNormalized { get; set; }

    public int? OemNumberId { get; set; }

    public string? OemErrorCode { get; set; }

    public bool IsDuplicate { get; set; }

    public int? VehicleConfigurationId { get; set; }

    public decimal VehicleMatchConfidence { get; set; }

    public string? Category { get; set; }

    public string? ImageUrl { get; set; }

    public string ReviewStatus { get; set; } = "Pending";

    public bool IsPublished { get; set; }

    public string? PublishError { get; set; }
}
