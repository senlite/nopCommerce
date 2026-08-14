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

    /// <summary>
    /// Inserts or updates OEM numbers in bulk, keyed on (ManufacturerId, NormalizedNumber) per FR-236.
    /// Callers must pre-normalize each entity. Existing rows keep their identity so relations and
    /// product maps that reference them are preserved.
    /// </summary>
    Task<OemBulkUpsertResult> BulkUpsertOemNumbersAsync(IReadOnlyList<OemNumber> numbers, CancellationToken cancellationToken);

    Task<IReadOnlyList<OemRelation>> GetRelationsAsync(CancellationToken cancellationToken);
    Task<OemRelation?> GetRelationByIdAsync(int id, CancellationToken cancellationToken);
    Task CreateRelationAsync(OemRelation entity, CancellationToken cancellationToken);
    Task UpdateRelationAsync(OemRelation entity, CancellationToken cancellationToken);
    Task DeleteRelationAsync(int id, CancellationToken cancellationToken);
}
