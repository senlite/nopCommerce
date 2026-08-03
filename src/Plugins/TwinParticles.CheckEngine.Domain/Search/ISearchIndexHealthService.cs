using System.Threading;
using System.Threading.Tasks;

namespace TwinParticles.CheckEngine.Domain.Search;

public interface ISearchIndexHealthService
{
    Task<bool> IsHealthyAsync(CancellationToken cancellationToken);

    Task RebuildAsync(CancellationToken cancellationToken);
}
