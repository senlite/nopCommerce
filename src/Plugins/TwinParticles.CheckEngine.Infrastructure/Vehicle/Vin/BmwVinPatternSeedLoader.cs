using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TwinParticles.CheckEngine.Domain.Vehicle;
using TwinParticles.CheckEngine.Domain.Vehicle.Admin;
using TwinParticles.CheckEngine.Infrastructure.Vehicle.Admin;

namespace TwinParticles.CheckEngine.Infrastructure.Vehicle.Vin;

public sealed class BmwVinPatternSeedLoader
{
    private readonly IVehicleAdminRepository _vehicleRepository;
    private readonly IVinSupportRepository _vinRepository;

    public BmwVinPatternSeedLoader(IVehicleAdminRepository vehicleRepository, IVinSupportRepository vinRepository)
    {
        _vehicleRepository = vehicleRepository;
        _vinRepository = vinRepository;
    }

    public async Task<int> SeedAsync(CancellationToken cancellationToken)
    {
        var catalog = await BmwVinPatternCatalog.LoadAsync(cancellationToken);
        var makes = await _vehicleRepository.GetMakesAsync(cancellationToken);
        var bmwMake = makes.FirstOrDefault(make => make.Code == BmwReferenceCatalog.MakeCode);
        var makeId = bmwMake?.Id;

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
