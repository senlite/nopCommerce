using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TwinParticles.CheckEngine.Domain.Oem;
using TwinParticles.CheckEngine.Domain.Oem.Admin;

namespace TwinParticles.CheckEngine.Application.Oem;

public sealed class OemAdminService
{
    // Supersession chains are linear and short in practice; this cap bounds the write-time walk and
    // matches the resolve-time depth guard so a pre-existing malformed chain can never spin forever.
    private const int MaxSupersessionWalkDepth = 64;

    private readonly IOemAdminRepository _repository;
    private readonly IOemNormalizationService _normalizationService;
    private readonly IOemRelationReadRepository? _relationReadRepository;

    public OemAdminService(
        IOemAdminRepository repository,
        IOemNormalizationService normalizationService,
        IOemRelationReadRepository? relationReadRepository = null)
    {
        _repository = repository;
        _normalizationService = normalizationService;
        _relationReadRepository = relationReadRepository;
    }

    public Task<IReadOnlyList<Manufacturer>> GetManufacturersAsync(CancellationToken cancellationToken) => _repository.GetManufacturersAsync(cancellationToken);
    public Task<Manufacturer?> GetManufacturerByIdAsync(int id, CancellationToken cancellationToken) => _repository.GetManufacturerByIdAsync(id, cancellationToken);

    public Task CreateManufacturerAsync(Manufacturer entity, CancellationToken cancellationToken)
    {
        EnsureManufacturerInvariant(entity);
        return _repository.CreateManufacturerAsync(entity, cancellationToken);
    }

    public Task UpdateManufacturerAsync(Manufacturer entity, CancellationToken cancellationToken)
    {
        EnsureManufacturerInvariant(entity);
        return _repository.UpdateManufacturerAsync(entity, cancellationToken);
    }

    public Task DeleteManufacturerAsync(int id, CancellationToken cancellationToken) => _repository.DeleteManufacturerAsync(id, cancellationToken);

    public Task<IReadOnlyList<OemNumber>> GetOemNumbersAsync(CancellationToken cancellationToken) => _repository.GetOemNumbersAsync(cancellationToken);
    public Task<OemNumber?> GetOemNumberByIdAsync(int id, CancellationToken cancellationToken) => _repository.GetOemNumberByIdAsync(id, cancellationToken);

    public Task CreateOemNumberAsync(OemNumber entity, CancellationToken cancellationToken)
    {
        EnsureOemNumberInvariant(entity);
        entity.NormalizedNumber = _normalizationService.Normalize(entity.DisplayNumber);

        if (string.IsNullOrWhiteSpace(entity.NormalizedNumber))
            throw new ArgumentException("oem.number.invalid", nameof(entity));

        return _repository.CreateOemNumberAsync(entity, cancellationToken);
    }

    public Task UpdateOemNumberAsync(OemNumber entity, CancellationToken cancellationToken)
    {
        EnsureOemNumberInvariant(entity);
        entity.NormalizedNumber = _normalizationService.Normalize(entity.DisplayNumber);

        if (string.IsNullOrWhiteSpace(entity.NormalizedNumber))
            throw new ArgumentException("oem.number.invalid", nameof(entity));

        return _repository.UpdateOemNumberAsync(entity, cancellationToken);
    }

    public Task DeleteOemNumberAsync(int id, CancellationToken cancellationToken) => _repository.DeleteOemNumberAsync(id, cancellationToken);

    /// <summary>
    /// Normalizes and bulk upserts OEM numbers keyed on (ManufacturerId, NormalizedNumber) per FR-236.
    /// Duplicate keys inside the batch collapse to the last occurrence so the upsert is deterministic.
    /// </summary>
    public Task<OemBulkUpsertResult> BulkUpsertOemNumbersAsync(IReadOnlyList<OemNumber> numbers, CancellationToken cancellationToken)
    {
        if (numbers is null)
            throw new ArgumentNullException(nameof(numbers));

        if (numbers.Count == 0)
            return Task.FromResult(OemBulkUpsertResult.Empty);

        var deduped = new Dictionary<(int ManufacturerId, string NormalizedNumber), OemNumber>();
        foreach (var entity in numbers)
        {
            EnsureOemNumberInvariant(entity);
            entity.NormalizedNumber = _normalizationService.Normalize(entity.DisplayNumber);
            if (string.IsNullOrWhiteSpace(entity.NormalizedNumber))
                throw new ArgumentException("oem.number.invalid", nameof(numbers));

            // Last write wins for a repeated (manufacturer, normalized) key within the same batch.
            deduped[(entity.ManufacturerId, entity.NormalizedNumber)] = entity;
        }

        return _repository.BulkUpsertOemNumbersAsync(deduped.Values.ToList(), cancellationToken);
    }

    public Task<IReadOnlyList<OemRelation>> GetRelationsAsync(CancellationToken cancellationToken) => _repository.GetRelationsAsync(cancellationToken);
    public Task<OemRelation?> GetRelationByIdAsync(int id, CancellationToken cancellationToken) => _repository.GetRelationByIdAsync(id, cancellationToken);

    public async Task CreateRelationAsync(OemRelation entity, CancellationToken cancellationToken)
    {
        EnsureRelationInvariant(entity);
        await EnsureSupersessionSafeAsync(entity, cancellationToken);
        await _repository.CreateRelationAsync(entity, cancellationToken);
    }

    public async Task UpdateRelationAsync(OemRelation entity, CancellationToken cancellationToken)
    {
        EnsureRelationInvariant(entity);
        await EnsureSupersessionSafeAsync(entity, cancellationToken);
        await _repository.UpdateRelationAsync(entity, cancellationToken);
    }

    public Task DeleteRelationAsync(int id, CancellationToken cancellationToken) => _repository.DeleteRelationAsync(id, cancellationToken);

    private static void EnsureManufacturerInvariant(Manufacturer entity)
    {
        if (entity is null)
            throw new ArgumentNullException(nameof(entity));
        if (string.IsNullOrWhiteSpace(entity.Code))
            throw new ArgumentException("oem.manufacturer.code_required", nameof(entity));
        if (string.IsNullOrWhiteSpace(entity.Name))
            throw new ArgumentException("oem.manufacturer.name_required", nameof(entity));
    }

    private static void EnsureOemNumberInvariant(OemNumber entity)
    {
        if (entity is null)
            throw new ArgumentNullException(nameof(entity));
        if (entity.ManufacturerId <= 0)
            throw new ArgumentException("oem.manufacturer.required", nameof(entity));
        if (string.IsNullOrWhiteSpace(entity.DisplayNumber))
            throw new ArgumentException("oem.display_number.required", nameof(entity));
    }

    private static void EnsureRelationInvariant(OemRelation entity)
    {
        if (entity is null)
            throw new ArgumentNullException(nameof(entity));
        if (entity.FromOemNumberId <= 0 || entity.ToOemNumberId <= 0)
            throw new ArgumentException("oem.relation.endpoint_required", nameof(entity));
        if (entity.FromOemNumberId == entity.ToOemNumberId)
            throw new ArgumentException("oem.relation.invalid_self_reference", nameof(entity));
    }

    /// <summary>
    /// Enforces FR-224/FR-225: supersession is directed, linear and transitively resolvable, never
    /// bidirectional or cyclic. Only active supersession edges are constrained; other relation types
    /// (cross-reference, equivalent, alternate, kit) may form arbitrary graphs.
    /// </summary>
    private async Task EnsureSupersessionSafeAsync(OemRelation entity, CancellationToken cancellationToken)
    {
        if (_relationReadRepository is null
            || entity.RelationType != OemRelationType.Supersession
            || !entity.IsActive)
            return;

        // A part supersedes at most one successor so the chain stays linear and resolvable.
        var sourceOutgoing = await _relationReadRepository.GetActiveOutgoingRelationsAsync(entity.FromOemNumberId, cancellationToken);
        if (sourceOutgoing.Any(relation =>
                relation.RelationType == OemRelationType.Supersession
                && relation.ToOemNumberId != entity.ToOemNumberId))
            throw new InvalidOperationException("oem.supersession.source_already_superseded");

        // A direct reverse edge (To supersedes From) would make the pair bidirectional.
        var targetOutgoing = await _relationReadRepository.GetActiveOutgoingRelationsAsync(entity.ToOemNumberId, cancellationToken);
        if (targetOutgoing.Any(relation =>
                relation.RelationType == OemRelationType.Supersession
                && relation.ToOemNumberId == entity.FromOemNumberId))
            throw new InvalidOperationException("oem.supersession.bidirectional");

        // Walk the existing chain forward from To; if it can already reach From, adding From->To closes a loop.
        var visited = new HashSet<int> { entity.ToOemNumberId };
        var current = entity.ToOemNumberId;
        for (var depth = 0; depth < MaxSupersessionWalkDepth; depth++)
        {
            var outgoing = await _relationReadRepository.GetActiveOutgoingRelationsAsync(current, cancellationToken);
            var next = outgoing.FirstOrDefault(relation => relation.RelationType == OemRelationType.Supersession);
            if (next is null)
                return;

            if (next.ToOemNumberId == entity.FromOemNumberId)
                throw new InvalidOperationException("oem.supersession.cycle");

            // A pre-existing loop unrelated to From is caught (and reported) at resolve time; stop here.
            if (!visited.Add(next.ToOemNumberId))
                return;

            current = next.ToOemNumberId;
        }

        throw new InvalidOperationException("oem.supersession.depth_exceeded");
    }
}
