namespace TwinParticles.CheckEngine.Models;

public sealed class ReferenceScaleLoadRequestModel
{
    public double ScaleFactor { get; set; } = 1.0;

    public bool ReplaceExisting { get; set; }

    public bool EnsureBmwVehicleSeed { get; set; } = true;
}
