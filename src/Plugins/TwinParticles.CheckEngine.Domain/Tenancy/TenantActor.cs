namespace TwinParticles.CheckEngine.Domain.Tenancy;

public sealed class TenantActor
{
    public int? TenantId { get; init; }

    public bool IsControlPlaneOperator { get; init; }

    public bool CanBypassIsolation => IsControlPlaneOperator;

    public static TenantActor ControlPlaneOperator { get; } = new() { IsControlPlaneOperator = true };

    public static TenantActor ForTenant(int tenantId) => new() { TenantId = tenantId };

    public static TenantActor Anonymous { get; } = new();
}
