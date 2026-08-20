using System;

namespace TwinParticles.CheckEngine.Domain.Tenancy;

/// <summary>
/// FR-1312: distributed cache keys are tenant-qualified. A collision across tenants is a security
/// defect, not a performance bug.
/// </summary>
public static class TenantCacheKey
{
    public const int UnresolvedTenantId = 0;

    public const string Prefix = "ce:t";

    public static string Qualify(int tenantId, string key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        return Prefix + tenantId.ToString(System.Globalization.CultureInfo.InvariantCulture) + ":" + key;
    }

    public static bool SharesTenant(string left, string right)
    {
        if (string.IsNullOrWhiteSpace(left) || string.IsNullOrWhiteSpace(right))
            return false;

        return TenantPrefix(left).Equals(TenantPrefix(right), StringComparison.Ordinal);
    }

    private static string TenantPrefix(string qualifiedKey)
    {
        var secondColon = qualifiedKey.IndexOf(':', Prefix.Length);
        return secondColon < 0 ? qualifiedKey : qualifiedKey[..secondColon];
    }
}
