namespace TwinParticles.CheckEngine.Domain.Marketplace;

/// <summary>
/// FR-860 supplier scorecard metrics (0..1 ratios; null when insufficient data).
/// </summary>
public sealed class VendorScorecard
{
    public int? VendorId { get; init; }

    public string? VendorName { get; init; }

    public decimal? FillRate { get; init; }

    public decimal? CancelRate { get; init; }

    public decimal? ClaimRejectionRate { get; init; }

    public decimal? OnTimeShipmentRate { get; init; }

    public int OrderSampleSize { get; init; }

    public int FitmentClaimSampleSize { get; init; }

    public int ShipmentSampleSize { get; init; }

    public static VendorScorecard Empty(int? vendorId = null, string? vendorName = null)
        => new() { VendorId = vendorId, VendorName = vendorName };
}
