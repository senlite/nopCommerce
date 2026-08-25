using System.Threading;
using System.Threading.Tasks;
using TwinParticles.CheckEngine.Domain.Vehicle.Admin;
using TwinParticles.CheckEngine.Infrastructure.Vehicle.Vin;

namespace TwinParticles.CheckEngine.Infrastructure.Vehicle.Admin;

public sealed class TopBrandReferenceVehicleSeedLoader
{
    private readonly IVehicleAdminRepository _repository;
    private readonly VinPatternSeedLoader _vinPatternSeedLoader;

    public TopBrandReferenceVehicleSeedLoader(
        IVehicleAdminRepository repository,
        VinPatternSeedLoader vinPatternSeedLoader)
    {
        _repository = repository;
        _vinPatternSeedLoader = vinPatternSeedLoader;
    }

    public async Task<VehicleSeedLoadResult> SeedAsync(CancellationToken cancellationToken)
    {
        var result = new VehicleSeedLoadResult();
        var document = await TopBrandReferenceCatalog.LoadAsync(cancellationToken);

        foreach (var catalog in document.Catalogs)
        {
            var loaded = await new ManufacturerReferenceVehicleSeedLoader(_repository, catalog)
                .SeedAsync(cancellationToken);
            Add(result, loaded);
        }

        await _vinPatternSeedLoader.SeedAllAsync(cancellationToken, includeBmw: false);
        return result;
    }

    private static void Add(VehicleSeedLoadResult target, VehicleSeedLoadResult source)
    {
        target.MakesInserted += source.MakesInserted;
        target.ModelsInserted += source.ModelsInserted;
        target.GenerationsInserted += source.GenerationsInserted;
        target.BodiesInserted += source.BodiesInserted;
        target.EnginesInserted += source.EnginesInserted;
        target.MarketsInserted += source.MarketsInserted;
        target.ConfigurationsInserted += source.ConfigurationsInserted;
        target.AliasesInserted += source.AliasesInserted;
    }
}
