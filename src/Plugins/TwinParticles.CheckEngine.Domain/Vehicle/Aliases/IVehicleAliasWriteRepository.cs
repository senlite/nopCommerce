using System.Threading;
using System.Threading.Tasks;
using TwinParticles.CheckEngine.Domain.Vehicle;

namespace TwinParticles.CheckEngine.Domain.Vehicle.Aliases;

public interface IVehicleAliasWriteRepository
{
    Task UpsertAsync(VehicleAlias alias, CancellationToken cancellationToken);
}
