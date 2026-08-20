using TwinParticles.CheckEngine.Domain.Tenancy;

namespace TwinParticles.CheckEngine.Infrastructure.Tenancy;

public sealed class TenantContextAccessor : ITenantAccessor
{
    private static readonly AsyncLocal<TenantContext?> Holder = new();

    public TenantContext Current => Holder.Value ?? TenantContext.Unresolved;

    public void SetCurrent(TenantContext context)
        => Holder.Value = context ?? TenantContext.Unresolved;
}
