namespace TwinParticles.CheckEngine.Domain.ReferenceScale;

public sealed class ReferenceScaleLoadRequest
{
    /// <summary>1.0 loads the full NFR reference counts; smaller values are for rehearsals/tests.</summary>
    public double ScaleFactor { get; set; } = 1.0;

    /// <summary>When true, existing tagged synthetic rows are removed before loading.</summary>
    public bool ReplaceExisting { get; set; }

    /// <summary>When true, the loader seeds the curated BMW hierarchy (H1.4) before expansion.</summary>
    public bool EnsureBmwVehicleSeed { get; set; } = true;
}
