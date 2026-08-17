using System;

namespace TwinParticles.CheckEngine.Domain.Marketplace;

/// <summary>
/// Vendor attribution on fitment claims until H3.7 adds a dedicated column.
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
