using System;

namespace TwinParticles.CheckEngine.Domain.ReferenceScale;

public static class ReferenceScaleDataGenerator
{
    public static string ProductSku(int sequence) => $"{ReferenceScaleManifest.ProductSkuPrefix}{sequence:D8}";

    public static string ProductName(int sequence) => $"Reference BMW Part {sequence}";

    public static string OemDisplayNumber(int sequence) =>
        $"{ReferenceScaleManifest.OemNormalizedPrefix}-{sequence:D8}";

    public static string OemNormalizedNumber(int sequence) =>
        $"{ReferenceScaleManifest.OemNormalizedPrefix}{sequence:D8}";

    public static string ConfigurationFingerprint(int sequence) =>
        $"{ReferenceScaleManifest.ConfigurationFingerprintPrefix}{sequence:D8}";

    public static string FitmentSourceReference(int productSequence, int claimSlot) =>
        $"{ReferenceScaleManifest.ProvenancePrefix}:claim:{productSequence:D8}:{claimSlot}";

    public static int ResolveConfigurationIndex(int productIndex, int claimSlot, int configurationCount)
    {
        if (configurationCount <= 0)
            throw new ArgumentOutOfRangeException(nameof(configurationCount));

        var combined = (long)productIndex * ReferenceScaleManifest.ClaimsPerProduct + claimSlot;
        return (int)(combined % configurationCount);
    }
}
