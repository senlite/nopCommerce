using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TwinParticles.CheckEngine.Domain.Tenancy;

namespace TwinParticles.CheckEngine.Application.Tenancy;

public sealed class TenantJobScope : ITenantJobScope
{
    private readonly ITenantRegistry _registry;

    public TenantJobScope(ITenantRegistry registry)
    {
        _registry = registry;
    }

    public async Task<IReadOnlyList<int>> ListTenantIdsForJobAsync(TenantActor actor, CancellationToken cancellationToken)
    {
        if (actor.CanBypassIsolation)
        {
            var all = await _registry.ListAsync(cancellationToken);
            return all.Where(t => t.Status == TenantStatus.Active).Select(t => t.Id).ToList();
        }

        if (actor.TenantId is int tenantId)
            return [tenantId];

        return [];
    }
}
