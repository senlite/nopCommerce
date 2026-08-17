using System;

namespace TwinParticles.CheckEngine.Domain.Fitment;

public sealed class FitmentClaim
{
    public int Id { get; set; }

    public int ProductId { get; set; }

    public int VehicleConfigurationId { get; set; }

    public int? OemNumberId { get; set; }

    /// <summary>
    /// FR-856: null means operator or system claim; set for vendor-contributed fitment.
    /// </summary>
    public int? VendorId { get; set; }

    public FitmentStatus Status { get; set; } = FitmentStatus.Unknown;

    public FitmentClaimQualifier Qualifier { get; set; } = new();

    public decimal Confidence { get; set; }

    public FitmentClaimProvenance Provenance { get; set; } = new();

    public SafetyClass SafetyClass { get; set; } = SafetyClass.Standard;

    public bool IsPublished { get; set; }

    public DateTime? ValidFromUtc { get; set; }

    public DateTime? ValidToUtc { get; set; }

    public bool IsActive { get; set; } = true;
}
