namespace TwinParticles.CheckEngine.Domain.Tenancy;

public sealed class TenantContext
{
    public int TenantId { get; init; }

    public string Slug { get; init; } = string.Empty;

    public TenantIsolationMode IsolationMode { get; init; }

    public string ConnectionName { get; init; } = string.Empty;

    public bool IsControlPlane { get; init; }

    public bool IsSelfHosted { get; init; }

    public static TenantContext Unresolved { get; } = new() { TenantId = TenantCacheKey.UnresolvedTenantId };
}
