namespace TwinParticles.CheckEngine.Domain.Vehicle;

public sealed class VinDecodeCandidate
{
    public int VehicleConfigurationId { get; init; }

    public Confidence Confidence { get; init; } = Confidence.Create(0m);

    public int? ModelYear { get; init; }
}
