using System.Threading;
using TwinParticles.CheckEngine.Domain.Tenancy;

namespace TwinParticles.CheckEngine.Infrastructure.Tenancy;

public sealed class TenantContextAccessor : ITenantAccessor
{
    private readonly AsyncLocal<TenantContext?> _holder = new();

    public TenantContext Current => _holder.Value ?? TenantContext.Unresolved;

    public void SetCurrent(TenantContext context)
        => _holder.Value = context ?? TenantContext.Unresolved;
}
