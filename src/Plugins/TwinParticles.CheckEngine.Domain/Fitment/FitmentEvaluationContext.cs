namespace TwinParticles.CheckEngine.Domain.Fitment;

public sealed class FitmentEvaluationContext
{
    public int ProductId { get; set; }

    public int VehicleConfigurationId { get; set; }

    public int? ProductionYear { get; set; }

    public string? SteeringSide { get; set; }

    public string? MarketRegion { get; set; }

    public string? DriveType { get; set; }

    public string? TransmissionType { get; set; }
}
