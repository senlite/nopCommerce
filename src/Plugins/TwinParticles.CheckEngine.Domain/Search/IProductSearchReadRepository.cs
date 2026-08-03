using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace TwinParticles.CheckEngine.Domain.Search;

public interface IProductSearchReadRepository
{
    Task<IReadOnlyList<SearchHit>> SearchKeywordAsync(SearchQuery query, CancellationToken cancellationToken);

    Task<IReadOnlyList<SearchHit>> SearchByCategoryAsync(SearchQuery query, CancellationToken cancellationToken);

    Task<IReadOnlyList<SearchHit>> SearchByVehicleTreeAsync(SearchQuery query, CancellationToken cancellationToken);

    Task<IReadOnlyList<SearchHit>> SearchByOemIdAsync(int oemNumberId, SearchQuery query, CancellationToken cancellationToken);
}
