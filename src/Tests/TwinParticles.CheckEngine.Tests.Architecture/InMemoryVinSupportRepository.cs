using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TwinParticles.CheckEngine.Domain.Vehicle;

namespace TwinParticles.CheckEngine.Tests.Architecture;

internal sealed class InMemoryVinSupportRepository : IVinSupportRepository
{
    private readonly List<VinWmi> _wmis = [];
    private readonly List<VinPattern> _patterns = [];
    private int _nextId = 1;

    public Task<IReadOnlyList<VinWmi>> GetWmisAsync(CancellationToken cancellationToken)
        => Task.FromResult<IReadOnlyList<VinWmi>>(_wmis.ToList());

    public Task<IReadOnlyList<VinPattern>> GetPatternsAsync(CancellationToken cancellationToken)
        => Task.FromResult<IReadOnlyList<VinPattern>>(_patterns.ToList());

    public Task UpsertWmiAsync(VinWmi entity, CancellationToken cancellationToken)
    {
        var existing = _wmis.FirstOrDefault(wmi => wmi.Wmi == entity.Wmi);
        if (existing is null)
        {
            entity.Id = _nextId++;
            _wmis.Add(Clone(entity));
        }
        else
        {
            existing.MakeId = entity.MakeId;
            existing.ManufacturerName = entity.ManufacturerName;
            existing.RegionCode = entity.RegionCode;
            existing.IsActive = entity.IsActive;
        }

        return Task.CompletedTask;
    }

    public Task UpsertPatternAsync(VinPattern entity, CancellationToken cancellationToken)
    {
        var existing = _patterns.FirstOrDefault(pattern =>
            pattern.Pattern == entity.Pattern
            && pattern.ModelCode == entity.ModelCode
            && pattern.GenerationCode == entity.GenerationCode
            && pattern.EngineCode == entity.EngineCode);

        if (existing is null)
        {
            entity.Id = _nextId++;
            _patterns.Add(Clone(entity));
        }
        else
        {
            existing.Priority = entity.Priority;
            existing.TrimSlug = entity.TrimSlug;
            existing.Confidence = entity.Confidence;
            existing.Provenance = entity.Provenance;
            existing.IsActive = entity.IsActive;
        }

        return Task.CompletedTask;
    }

    private static VinWmi Clone(VinWmi entity)
        => new()
        {
            Id = entity.Id,
            Wmi = entity.Wmi,
            MakeId = entity.MakeId,
            ManufacturerName = entity.ManufacturerName,
            RegionCode = entity.RegionCode,
            IsActive = entity.IsActive
        };

    private static VinPattern Clone(VinPattern entity)
        => new()
        {
            Id = entity.Id,
            MakeId = entity.MakeId,
            Pattern = entity.Pattern,
            Priority = entity.Priority,
            ModelCode = entity.ModelCode,
            GenerationCode = entity.GenerationCode,
            EngineCode = entity.EngineCode,
            TrimSlug = entity.TrimSlug,
            Confidence = entity.Confidence,
            Provenance = entity.Provenance,
            IsActive = entity.IsActive
        };
}
