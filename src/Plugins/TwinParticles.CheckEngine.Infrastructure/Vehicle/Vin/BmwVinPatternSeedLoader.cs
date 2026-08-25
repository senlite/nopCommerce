using System.Threading;
using System.Threading.Tasks;
using TwinParticles.CheckEngine.Domain.Vehicle;
using TwinParticles.CheckEngine.Domain.Vehicle.Admin;
using TwinParticles.CheckEngine.Infrastructure.Vehicle.Admin;

namespace TwinParticles.CheckEngine.Infrastructure.Vehicle.Vin;

public sealed class BmwVinPatternSeedLoader
{
    private readonly VinPatternSeedLoader _seedLoader;

    public BmwVinPatternSeedLoader(IVehicleAdminRepository vehicleRepository, IVinSupportRepository vinRepository)
    {
        _seedLoader = new VinPatternSeedLoader(vehicleRepository, vinRepository);
    }

    public async Task<int> SeedAsync(CancellationToken cancellationToken)
    {
        var catalog = await BmwVinPatternCatalog.LoadAsync(cancellationToken);
        return await _seedLoader.SeedAsync(catalog, cancellationToken);
    }
}
