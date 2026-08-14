namespace TwinParticles.CheckEngine.Domain.ReferenceScale;

public sealed class ReferenceScaleStatus
{
    public int ProductCount { get; set; }

    public int FitmentClaimCount { get; set; }

    public int ConfigurationCount { get; set; }

    public int OemEntryCount { get; set; }

    public int ProductOemMapCount { get; set; }

    public bool IsFullyLoaded { get; set; }
}
