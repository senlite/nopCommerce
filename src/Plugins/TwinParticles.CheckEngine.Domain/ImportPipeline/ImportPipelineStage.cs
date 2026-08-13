namespace TwinParticles.CheckEngine.Domain.ImportPipeline;

/// <summary>
/// Durable import stage identifiers. Values are stable because completed stages are persisted by
/// name and exposed through the admin API.
/// </summary>
public enum ImportPipelineStage
{
    Extract = 1,
    Normalize = 2,
    Deduplicate = 3,
    OemMatch = 4,
    VehicleMatch = 5,
    Enrich = 6,
    Translate = 7,
    SeoGenerate = 8,
    Categorize = 9,
    ImageAssign = 10,
    Review = 11,
    Publish = 12
}
