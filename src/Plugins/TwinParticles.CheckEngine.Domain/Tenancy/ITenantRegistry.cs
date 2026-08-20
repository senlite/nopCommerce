using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace TwinParticles.CheckEngine.Domain.Tenancy;

public interface ITenantRegistry
{
    Task<Tenant?> GetByIdAsync(int tenantId, CancellationToken cancellationToken);

    Task<Tenant?> GetBySlugAsync(string slug, CancellationToken cancellationToken);

    Task<Tenant?> GetByHostnameAsync(string hostname, CancellationToken cancellationToken);

    Task<IReadOnlyList<Tenant>> ListAsync(CancellationToken cancellationToken);

    Task<int> InsertAsync(Tenant tenant, CancellationToken cancellationToken);

    Task UpdateAsync(Tenant tenant, CancellationToken cancellationToken);
}
