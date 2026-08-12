using System;
using System.Collections.Generic;
using TwinParticles.CheckEngine.Domain.ImportPipeline;

namespace TwinParticles.CheckEngine.Application.ImportPipeline.Orchestration;

public sealed class ImportPipelineBatchState
{
    public Guid BatchId { get; init; }

    public int? SqlBatchId { get; set; }

    public string FileName { get; init; } = string.Empty;

    public ImportSourceFormat Format { get; init; }

    public bool DryRun { get; init; }

    public string Status { get; set; } = "Received";

    public DateTimeOffset CreatedUtc { get; init; }

    public int TotalRows { get; set; }

    public int PublishedRows { get; set; }

    public int FailedRows { get; set; }

    public List<ImportPipelineRowState> Rows { get; init; } = [];
}
