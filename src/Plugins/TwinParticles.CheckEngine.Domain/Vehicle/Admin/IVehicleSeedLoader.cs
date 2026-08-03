using System.Threading;
using System.Threading.Tasks;

namespace TwinParticles.CheckEngine.Domain.Vehicle.Admin;

public interface IVehicleSeedLoader
{
    Task<VehicleSeedLoadResult> SeedAsync(CancellationToken cancellationToken);
}
