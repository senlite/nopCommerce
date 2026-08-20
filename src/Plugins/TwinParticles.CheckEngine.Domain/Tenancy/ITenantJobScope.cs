using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace TwinParticles.CheckEngine.Domain.Tenancy;

/// <summary>
/// FR-1311: background work must enumerate tenants explicitly. Accidental cross-tenant fan-out is a
/// security defect.
/// </summary>
public interface ITenantJobScope
{
    Task<IReadOnlyList<int>> ListTenantIdsForJobAsync(TenantActor actor, CancellationToken cancellationToken);
}
