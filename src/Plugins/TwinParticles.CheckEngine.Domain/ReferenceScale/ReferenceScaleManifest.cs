using System;

namespace TwinParticles.CheckEngine.Domain.ReferenceScale;

/// <summary>
/// Canonical counts for the NFR reference environment ([03] § reference dataset, H1.11).
/// Synthetic rows are tagged with <see cref="ProvenancePrefix"/> so operators can purge rehearsals.
/// </summary>
public static class ReferenceScaleManifest
{
    public const string Version = "v1";
    public const string ProvenancePrefix = "ref-scale:v1";
    public const string ProductSkuPrefix = "REF-SCALE-";
    public const string OemNormalizedPrefix = "REFSCALE";
    public const string ConfigurationFingerprintPrefix = "ref-scale:v1:cfg:";
    public const string CreatedBy = "reference-scale-loader";

    public const int TargetProducts = 250_000;
    public const int TargetFitmentClaims = 2_000_000;
    public const int TargetConfigurations = 40_000;
    public const int TargetOemEntries = 500_000;
    public const int ClaimsPerProduct = TargetFitmentClaims / TargetProducts;

    public static ReferenceScaleTargets ResolveTargets(double scaleFactor)
    {
        if (scaleFactor <= 0 || scaleFactor > 1)
            throw new ArgumentOutOfRangeException(nameof(scaleFactor), "Scale factor must be in (0, 1].");

        var products = Math.Max(1, (int)Math.Round(TargetProducts * scaleFactor));
        var claims = Math.Max(products, (int)Math.Round(TargetFitmentClaims * scaleFactor));
        var configurations = Math.Max(1, (int)Math.Round(TargetConfigurations * scaleFactor));
        var oemEntries = Math.Max(1, (int)Math.Round(TargetOemEntries * scaleFactor));

        return new ReferenceScaleTargets(products, claims, configurations, oemEntries);
    }
}

public readonly record struct ReferenceScaleTargets(
    int Products,
    int FitmentClaims,
    int Configurations,
    int OemEntries);
