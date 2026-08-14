namespace TwinParticles.CheckEngine.Models;

public sealed class FitmentEvaluateRequestModel
{
    public int ProductId { get; set; }

    public int VehicleConfigurationId { get; set; }

    public int? ProductionYear { get; set; }

    public string? SteeringSide { get; set; }

    public string? MarketRegion { get; set; }

    public string? DriveType { get; set; }

    public string? TransmissionType { get; set; }
}

public sealed class FitmentReviewActionModel
{
    public int ClaimId { get; set; }
}
