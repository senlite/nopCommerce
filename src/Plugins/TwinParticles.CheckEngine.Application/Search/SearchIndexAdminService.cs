using System.Threading;
using System.Threading.Tasks;

namespace TwinParticles.CheckEngine.Application.Search;

public sealed class SearchIndexAdminService
{
    private readonly UnifiedSearchService _unifiedSearchService;

    public SearchIndexAdminService(UnifiedSearchService unifiedSearchService)
    {
        _unifiedSearchService = unifiedSearchService;
    }

    public Task RebuildAsync(CancellationToken cancellationToken)
    {
        return _unifiedSearchService.RebuildIndexAsync(cancellationToken);
    }
}
