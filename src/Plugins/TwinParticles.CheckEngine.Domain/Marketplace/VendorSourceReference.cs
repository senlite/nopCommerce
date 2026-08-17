using System;

namespace TwinParticles.CheckEngine.Domain.Marketplace;

/// <summary>
/// Legacy source-reference encoding for vendor attribution. Prefer <see cref="FitmentClaim.VendorId"/>.
/// </summary>
public static class VendorSourceReference
{
    private const string Prefix = "vendor:";

    public static string ForVendor(int vendorId) => $"{Prefix}{vendorId}";

    public static bool TryParseVendorId(string? sourceReference, out int vendorId)
    {
        vendorId = 0;
        if (string.IsNullOrWhiteSpace(sourceReference) || !sourceReference.StartsWith(Prefix, StringComparison.Ordinal))
            return false;

        return int.TryParse(sourceReference.AsSpan(Prefix.Length), out vendorId);
    }
}
