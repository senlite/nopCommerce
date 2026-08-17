using System;

namespace TwinParticles.CheckEngine.Domain.Licensing;

/// <summary>
/// Tier-to-entitlement matrix (FR-870, AC-43.1). Marketplace is Business, Enterprise, and OEM.
/// </summary>
public static class LicenceTierEntitlements
{
    public static bool GrantsMarketplace(LicenceTier tier)
        => tier is LicenceTier.Business or LicenceTier.Enterprise or LicenceTier.OemRedistribution;

    public static LicenceTier ParseTier(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return LicenceTier.Unknown;

        var normalized = raw.Trim().Replace(" ", string.Empty).Replace("_", string.Empty).Replace("/", string.Empty);
        if (normalized.Equals("singlestore", StringComparison.OrdinalIgnoreCase))
            return LicenceTier.SingleStore;
        if (normalized.Equals("multistore", StringComparison.OrdinalIgnoreCase))
            return LicenceTier.MultiStore;
        if (normalized.Equals("business", StringComparison.OrdinalIgnoreCase))
            return LicenceTier.Business;
        if (normalized.Equals("enterprise", StringComparison.OrdinalIgnoreCase))
            return LicenceTier.Enterprise;
        if (normalized.Equals("oem", StringComparison.OrdinalIgnoreCase)
            || normalized.Equals("oemredistribution", StringComparison.OrdinalIgnoreCase))
            return LicenceTier.OemRedistribution;

        return LicenceTier.Unknown;
    }
}
