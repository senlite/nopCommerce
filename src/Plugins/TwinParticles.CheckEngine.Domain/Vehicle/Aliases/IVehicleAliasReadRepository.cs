using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace TwinParticles.CheckEngine.Domain.Vehicle.Aliases;

public interface IVehicleAliasReadRepository
{
    Task<IReadOnlyList<VehicleAliasSearchItem>> SearchAsync(VehicleAliasSearchCriteria criteria, CancellationToken cancellationToken);
}
