using System;

namespace TwinParticles.CheckEngine.Application.ImportPipeline.Orchestration;

public sealed class ImportPipelineRunResult
{
    public Guid BatchId { get; init; }

    public string Status { get; init; } = string.Empty;

    public int TotalRows { get; init; }

    public int ReviewRows { get; init; }
}
