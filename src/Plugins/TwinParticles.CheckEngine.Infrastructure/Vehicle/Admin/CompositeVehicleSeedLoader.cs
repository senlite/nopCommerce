using System.Threading;
using System.Threading.Tasks;
using TwinParticles.CheckEngine.Domain.Vehicle.Admin;

namespace TwinParticles.CheckEngine.Infrastructure.Vehicle.Admin;

public sealed class CompositeVehicleSeedLoader : IVehicleSeedLoader
{
    private readonly BmwReferenceVehicleSeedLoader _bmwLoader;
    private readonly TopBrandReferenceVehicleSeedLoader _topBrandLoader;

    public CompositeVehicleSeedLoader(
        BmwReferenceVehicleSeedLoader bmwLoader,
        TopBrandReferenceVehicleSeedLoader topBrandLoader)
    {
        _bmwLoader = bmwLoader;
        _topBrandLoader = topBrandLoader;
    }

    public async Task<VehicleSeedLoadResult> SeedAsync(CancellationToken cancellationToken)
    {
        var bmw = await _bmwLoader.SeedAsync(cancellationToken);
        var others = await _topBrandLoader.SeedAsync(cancellationToken);
        return Combine(bmw, others);
    }

    private static VehicleSeedLoadResult Combine(VehicleSeedLoadResult left, VehicleSeedLoadResult right)
        => new()
        {
            MakesInserted = left.MakesInserted + right.MakesInserted,
            ModelsInserted = left.ModelsInserted + right.ModelsInserted,
            GenerationsInserted = left.GenerationsInserted + right.GenerationsInserted,
            BodiesInserted = left.BodiesInserted + right.BodiesInserted,
            EnginesInserted = left.EnginesInserted + right.EnginesInserted,
            MarketsInserted = left.MarketsInserted + right.MarketsInserted,
            ConfigurationsInserted = left.ConfigurationsInserted + right.ConfigurationsInserted,
            AliasesInserted = left.AliasesInserted + right.AliasesInserted
        };
}
