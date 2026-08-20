namespace TwinParticles.CheckEngine.Domain.Tenancy;

public interface ITenantConnectionRouter
{
    /// <summary>
    /// Returns the connection name the caller may use for <paramref name="tenant"/>. Cross-tenant
    /// routing is denied unless the current context is the control plane.
    /// </summary>
    TenantConnectionDecision Resolve(Tenant tenant, TenantContext current);
}

public sealed class TenantConnectionDecision
{
    public bool Allowed { get; init; }

    public string ConnectionName { get; init; } = string.Empty;

    public string? ReasonCode { get; init; }

    public static TenantConnectionDecision Allow(string connectionName)
        => new() { Allowed = true, ConnectionName = connectionName ?? string.Empty };

    public static TenantConnectionDecision Deny(string reasonCode)
        => new() { Allowed = false, ReasonCode = reasonCode };
}
