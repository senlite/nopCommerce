using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TwinParticles.CheckEngine.Domain.Vehicle;
using TwinParticles.CheckEngine.Domain.Vehicle.Admin;
using TwinParticles.CheckEngine.Infrastructure.Vehicle.Admin;

namespace TwinParticles.CheckEngine.Infrastructure.Vehicle.Vin;

public sealed class VinPatternSeedLoader
{
    private readonly IVehicleAdminRepository _vehicleRepository;
    private readonly IVinSupportRepository _vinRepository;

    public VinPatternSeedLoader(IVehicleAdminRepository vehicleRepository, IVinSupportRepository vinRepository)
    {
        _vehicleRepository = vehicleRepository;
        _vinRepository = vinRepository;
    }

    public async Task<int> SeedAllAsync(CancellationToken cancellationToken, bool includeBmw = true)
    {
        var inserted = 0;
        foreach (var catalog in await VinPatternCatalog.LoadAllAsync(cancellationToken))
        {
            if (!includeBmw && VinPatternCatalog.IsBmw(catalog))
                continue;

            inserted += await SeedAsync(catalog, cancellationToken);
        }

        return inserted;
    }

    public async Task<int> SeedAsync(BmwVinPatternCatalogDocument catalog, CancellationToken cancellationToken)
    {
        var makeCode = string.IsNullOrWhiteSpace(catalog.MakeCode)
            ? BmwReferenceCatalog.MakeCode
            : catalog.MakeCode;
        var makes = await _vehicleRepository.GetMakesAsync(cancellationToken);
        var make = makes.FirstOrDefault(candidate =>
            string.Equals(candidate.Code, makeCode, StringComparison.OrdinalIgnoreCase));
        var makeId = make?.Id;

        var inserted = 0;
        foreach (var wmi in catalog.Wmis)
        {
            await _vinRepository.UpsertWmiAsync(new VinWmi
            {
                Wmi = wmi.Wmi,
                MakeId = makeId,
                ManufacturerName = wmi.ManufacturerName,
                RegionCode = wmi.RegionCode,
                IsActive = true
            }, cancellationToken);
            inserted++;
        }

        if (!makeId.HasValue)
            return inserted;

        foreach (var pattern in catalog.Patterns)
        {
            await _vinRepository.UpsertPatternAsync(new VinPattern
            {
                MakeId = makeId.Value,
                Pattern = pattern.Pattern,
                Priority = pattern.Priority,
                ModelCode = pattern.ModelCode,
                GenerationCode = pattern.GenerationCode,
                EngineCode = pattern.EngineCode,
                TrimSlug = pattern.TrimSlug,
                Confidence = pattern.Confidence,
                Provenance = pattern.Provenance,
                IsActive = true
            }, cancellationToken);
            inserted++;
        }

        return inserted;
    }
}
