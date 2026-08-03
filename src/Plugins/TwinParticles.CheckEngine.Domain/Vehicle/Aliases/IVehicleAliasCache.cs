using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace TwinParticles.CheckEngine.Domain.Vehicle.Aliases;

public interface IVehicleAliasCache
{
    Task<IReadOnlyList<VehicleAliasSearchItem>?> GetAsync(string term, string locale, int take, CancellationToken cancellationToken);

    Task SetAsync(string term, string locale, int take, IReadOnlyList<VehicleAliasSearchItem> items, CancellationToken cancellationToken);

    Task InvalidateAsync(string locale, CancellationToken cancellationToken);
}
