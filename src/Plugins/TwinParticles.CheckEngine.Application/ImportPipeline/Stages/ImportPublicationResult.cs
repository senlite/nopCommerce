namespace TwinParticles.CheckEngine.Application.ImportPipeline.Stages;

public sealed class ImportPublicationResult
{
    public int PublishedRows { get; init; }

    public int FailedRows { get; init; }

    public bool DryRun { get; init; }
}
