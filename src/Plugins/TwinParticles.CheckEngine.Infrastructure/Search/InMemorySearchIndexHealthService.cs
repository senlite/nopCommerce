using System.Threading;
using System.Threading.Tasks;
using TwinParticles.CheckEngine.Domain.Search;

namespace TwinParticles.CheckEngine.Infrastructure.Search;

public sealed class InMemorySearchIndexHealthService : ISearchIndexHealthService
{
    private bool _healthy = true;

    public Task<bool> IsHealthyAsync(CancellationToken cancellationToken)
    {
        return Task.FromResult(_healthy);
    }

    public Task RebuildAsync(CancellationToken cancellationToken)
    {
        _healthy = true;
        return Task.CompletedTask;
    }

    public void MarkDegradedForTestOnly()
    {
        _healthy = false;
    }
}
