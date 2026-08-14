namespace TwinParticles.CheckEngine.Domain.ReferenceScale;

public sealed class ReferenceScaleLoadResult
{
    public bool AlreadyLoaded { get; set; }

    public ReferenceScaleTargets Targets { get; set; }

    public int ConfigurationsInserted { get; set; }

    public int OemEntriesUpserted { get; set; }

    public int ProductsInserted { get; set; }

    public int FitmentClaimsInserted { get; set; }

    public int ProductOemMapsInserted { get; set; }

    public TimeSpan Elapsed { get; set; }
}
