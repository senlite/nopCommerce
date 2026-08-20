using TwinParticles.CheckEngine.Domain.Tenancy;

namespace TwinParticles.CheckEngine.Application.Tenancy;

public sealed class TenantConnectionRouter : ITenantConnectionRouter
{
    public TenantConnectionDecision Resolve(Tenant tenant, TenantContext current)
    {
        if (tenant is null)
            return TenantConnectionDecision.Deny(TenantErrorCodes.TenantNotFound);

        if (current.IsControlPlane || current.TenantId == tenant.Id)
            return TenantConnectionDecision.Allow(tenant.ConnectionName);

        return TenantConnectionDecision.Deny(TenantErrorCodes.CrossConnectionDenied);
    }
}
