using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace TwinParticles.CheckEngine.Domain.Vehicle;

public interface IVinSupportRepository
{
    Task<IReadOnlyList<VinWmi>> GetWmisAsync(CancellationToken cancellationToken);

    Task<IReadOnlyList<VinPattern>> GetPatternsAsync(CancellationToken cancellationToken);

    Task UpsertWmiAsync(VinWmi entity, CancellationToken cancellationToken);

    Task UpsertPatternAsync(VinPattern entity, CancellationToken cancellationToken);
}
