using TwinParticles.CheckEngine.Domain.ImportPipeline;

namespace TwinParticles.CheckEngine.Application.ImportPipeline.Orchestration;

public sealed class ImportPipelineRunRequest
{
    public ImportSourceFormat Format { get; init; }

    public string FileName { get; init; } = string.Empty;

    public byte[] Content { get; init; } = [];

    public string? SupplierProfileCode { get; init; }

    public bool DryRun { get; init; }

    public bool EnableAiEnrichment { get; init; }

    public bool EnableTranslation { get; init; }

    public bool EnableSeoGeneration { get; init; }
}
