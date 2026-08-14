using System;
using TwinParticles.CheckEngine.Domain.ImportPipeline;

namespace TwinParticles.CheckEngine.Application.ImportPipeline.Orchestration;

public sealed class ImportPipelineStageRunResult
{
    public Guid BatchId { get; init; }

    public ImportPipelineStage Stage { get; init; }

    public string Status { get; init; } = string.Empty;

    public int TotalRows { get; init; }

    public int FailedRows { get; init; }
}
