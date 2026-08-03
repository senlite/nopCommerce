using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TwinParticles.CheckEngine.Domain.Oem;
using TwinParticles.CheckEngine.Domain.Oem.Admin;

namespace TwinParticles.CheckEngine.Application.Oem;

public sealed class OemAdminService
{
    private readonly IOemAdminRepository _repository;
    private readonly IOemNormalizationService _normalizationService;

    public OemAdminService(IOemAdminRepository repository, IOemNormalizationService normalizationService)
    {
        _repository = repository;
        _normalizationService = normalizationService;
    }

    public Task<IReadOnlyList<Manufacturer>> GetManufacturersAsync(CancellationToken cancellationToken) => _repository.GetManufacturersAsync(cancellationToken);
    public Task<Manufacturer?> GetManufacturerByIdAsync(int id, CancellationToken cancellationToken) => _repository.GetManufacturerByIdAsync(id, cancellationToken);
    public Task CreateManufacturerAsync(Manufacturer entity, CancellationToken cancellationToken) => _repository.CreateManufacturerAsync(entity, cancellationToken);
    public Task UpdateManufacturerAsync(Manufacturer entity, CancellationToken cancellationToken) => _repository.UpdateManufacturerAsync(entity, cancellationToken);
    public Task DeleteManufacturerAsync(int id, CancellationToken cancellationToken) => _repository.DeleteManufacturerAsync(id, cancellationToken);

    public Task<IReadOnlyList<OemNumber>> GetOemNumbersAsync(CancellationToken cancellationToken) => _repository.GetOemNumbersAsync(cancellationToken);
    public Task<OemNumber?> GetOemNumberByIdAsync(int id, CancellationToken cancellationToken) => _repository.GetOemNumberByIdAsync(id, cancellationToken);

    public Task CreateOemNumberAsync(OemNumber entity, CancellationToken cancellationToken)
    {
        entity.NormalizedNumber = _normalizationService.Normalize(entity.DisplayNumber);
        return _repository.CreateOemNumberAsync(entity, cancellationToken);
    }

    public Task UpdateOemNumberAsync(OemNumber entity, CancellationToken cancellationToken)
    {
        entity.NormalizedNumber = _normalizationService.Normalize(entity.DisplayNumber);
        return _repository.UpdateOemNumberAsync(entity, cancellationToken);
    }

    public Task DeleteOemNumberAsync(int id, CancellationToken cancellationToken) => _repository.DeleteOemNumberAsync(id, cancellationToken);

    public Task<IReadOnlyList<OemRelation>> GetRelationsAsync(CancellationToken cancellationToken) => _repository.GetRelationsAsync(cancellationToken);
    public Task<OemRelation?> GetRelationByIdAsync(int id, CancellationToken cancellationToken) => _repository.GetRelationByIdAsync(id, cancellationToken);

    public Task CreateRelationAsync(OemRelation entity, CancellationToken cancellationToken)
    {
        EnsureRelationInvariant(entity);
        return _repository.CreateRelationAsync(entity, cancellationToken);
    }

    public Task UpdateRelationAsync(OemRelation entity, CancellationToken cancellationToken)
    {
        EnsureRelationInvariant(entity);
        return _repository.UpdateRelationAsync(entity, cancellationToken);
    }

    public Task DeleteRelationAsync(int id, CancellationToken cancellationToken) => _repository.DeleteRelationAsync(id, cancellationToken);

    private static void EnsureRelationInvariant(OemRelation entity)
    {
        if (entity.FromOemNumberId == entity.ToOemNumberId)
            throw new ArgumentException("oem.relation.invalid_self_reference", nameof(entity));
    }
}
