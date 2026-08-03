using System.Threading;
using System.Threading.Tasks;
using TwinParticles.CheckEngine.Domain.Search;

namespace TwinParticles.CheckEngine.Application.Search;

public sealed class GarageContextSearchService
{
    private readonly UnifiedSearchService _searchService;

    public GarageContextSearchService(UnifiedSearchService searchService)
    {
        _searchService = searchService;
    }

    public Task<SearchResult> SearchWithGarageContextAsync(SearchQuery query, int? activeVehicleConfigurationId, CancellationToken cancellationToken)
    {
        if (query.VehicleConfigurationId.HasValue || !activeVehicleConfigurationId.HasValue)
            return _searchService.SearchAsync(query, cancellationToken);

        return _searchService.SearchAsync(new SearchQuery
        {
            RawText = query.RawText,
            Mode = query.Mode,
            VehicleConfigurationId = activeVehicleConfigurationId,
            WidenFitment = query.WidenFitment,
            Filters = query.Filters,
            Page = query.Page,
            PageSize = query.PageSize,
            Locale = query.Locale
        }, cancellationToken);
    }
}
