using System.Threading;
using System.Threading.Tasks;
using TwinParticles.CheckEngine.Domain.Security;
using TwinParticles.CheckEngine.Domain.Tenancy;

namespace TwinParticles.CheckEngine.Application.Tenancy;

/// <summary>FR-1310 / AC-19.1 analogue: no tenant may read or write another tenant's resources.</summary>
public sealed class TenantIsolationService
{
    private readonly ICheckEngineAuditService _auditService;

    public TenantIsolationService(ICheckEngineAuditService auditService)
    {
        _auditService = auditService;
    }

    public async Task<TenantIsolationDecision> AuthorizeAsync(
        TenantActor actor,
        int targetTenantId,
        bool write,
        CancellationToken cancellationToken)
    {
        if (actor.TenantId is null && !actor.CanBypassIsolation)
            return TenantIsolationDecision.Deny(TenantErrorCodes.IsolationUnauthenticated);

        if (actor.CanBypassIsolation)
        {
            if (write)
            {
                await _auditService.AppendAsync(
                    "control-plane",
                    "tenant.isolation.elevated_write",
                    "Tenant",
                    targetTenantId.ToString(),
                    null,
                    null,
                    cancellationToken);
            }

            return TenantIsolationDecision.Allow();
        }

        if (actor.TenantId != targetTenantId)
        {
            await _auditService.AppendAsync(
                "tenant:" + actor.TenantId,
                write ? "tenant.isolation.write_denied" : "tenant.isolation.read_denied",
                "Tenant",
                targetTenantId.ToString(),
                null,
                null,
                cancellationToken);
            return TenantIsolationDecision.Deny(TenantErrorCodes.IsolationDenied);
        }

        return TenantIsolationDecision.Allow();
    }
}
