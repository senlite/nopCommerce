namespace TwinParticles.CheckEngine.Domain.Dealer;

public sealed class WarrantyClaim
{
    public int Id { get; set; }

    public int DealerAccountId { get; set; }

    public int? OrderId { get; set; }

    public string OemNumber { get; set; } = string.Empty;

    public int? ResolvedOemNumberId { get; set; }

    public int VehicleConfigurationId { get; set; }

    public WarrantyClaimStatus Status { get; set; }

    public string EvidenceJson { get; set; } = "[]";

    public DateTimeOffset CreatedUtc { get; set; }

    public DateTimeOffset UpdatedUtc { get; set; }
}
