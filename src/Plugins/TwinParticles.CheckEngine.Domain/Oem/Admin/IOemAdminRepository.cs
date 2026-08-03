using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace TwinParticles.CheckEngine.Domain.Oem.Admin;

public interface IOemAdminRepository
{
    Task<IReadOnlyList<Manufacturer>> GetManufacturersAsync(CancellationToken cancellationToken);
    Task<Manufacturer?> GetManufacturerByIdAsync(int id, CancellationToken cancellationToken);
    Task CreateManufacturerAsync(Manufacturer entity, CancellationToken cancellationToken);
    Task UpdateManufacturerAsync(Manufacturer entity, CancellationToken cancellationToken);
    Task DeleteManufacturerAsync(int id, CancellationToken cancellationToken);

    Task<IReadOnlyList<OemNumber>> GetOemNumbersAsync(CancellationToken cancellationToken);
    Task<OemNumber?> GetOemNumberByIdAsync(int id, CancellationToken cancellationToken);
    Task CreateOemNumberAsync(OemNumber entity, CancellationToken cancellationToken);
    Task UpdateOemNumberAsync(OemNumber entity, CancellationToken cancellationToken);
    Task DeleteOemNumberAsync(int id, CancellationToken cancellationToken);

    Task<IReadOnlyList<OemRelation>> GetRelationsAsync(CancellationToken cancellationToken);
    Task<OemRelation?> GetRelationByIdAsync(int id, CancellationToken cancellationToken);
    Task CreateRelationAsync(OemRelation entity, CancellationToken cancellationToken);
    Task UpdateRelationAsync(OemRelation entity, CancellationToken cancellationToken);
    Task DeleteRelationAsync(int id, CancellationToken cancellationToken);
}
