using System;
using TwinParticles.CheckEngine.Domain.ImportPipeline;

namespace TwinParticles.CheckEngine.Models;

public sealed class ImportAdminRunRequestModel
{
    public ImportSourceFormat Format { get; set; } = ImportSourceFormat.Csv;

    public string FileName { get; set; } = string.Empty;

    public string ContentBase64 { get; set; } = string.Empty;

    public string? SupplierProfileCode { get; set; }

    public bool DryRun { get; set; } = true;

    public bool EnableAiEnrichment { get; set; }

    public bool EnableTranslation { get; set; }

    public bool EnableSeoGeneration { get; set; }
}

public sealed class ImportAdminReviewStatusModel
{
    public Guid BatchId { get; set; }

    public int RowNumber { get; set; }

    public string ReviewStatus { get; set; } = "Approved";
}

public sealed class ImportAdminPublishModel
{
    public Guid BatchId { get; set; }

    public bool DryRun { get; set; }
}

public sealed class ImportAdminRerunStageModel
{
    public Guid BatchId { get; set; }

    public ImportPipelineStage Stage { get; set; }
}

public sealed class ImportAdminDuplicateDecisionModel
{
    public Guid BatchId { get; set; }

    public int RowNumber { get; set; }

    /// <summary>One of: Merge, Link, KeepSeparate.</summary>
    public string Decision { get; set; } = string.Empty;
}
