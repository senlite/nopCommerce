namespace TwinParticles.CheckEngine.Domain.Fitment;

public sealed class FitmentClaimQualifier
{
    public int? ProductionFromYear { get; set; }

    public int? ProductionToYear { get; set; }

    public string? SteeringSide { get; set; }

    public string? MarketRegion { get; set; }

    public string? DriveType { get; set; }

    public string? TransmissionType { get; set; }

    public string? OptionCodesCsv { get; set; }
}
