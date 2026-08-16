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

    // Lightweight prefix lookup for typeahead (FR-415). Declared as a default member so existing
    // in-memory test doubles keep compiling; production and in-memory repositories override it.
    Task<IReadOnlyList<SearchHit>> SuggestProductsAsync(string prefix, string locale, int take, CancellationToken cancellationToken)
        => Task.FromResult<IReadOnlyList<SearchHit>>([]);

    // Keywordless browse used for the unscoped recommendation rail when no vehicle context is set
    // (FR-550 "no context -> unscoped but labelled"). Keyword search cannot serve this because it
    // returns nothing for empty text. Bounded by <see cref="SearchQuery.PageSize"/>.
    Task<IReadOnlyList<SearchHit>> BrowseUnscopedAsync(SearchQuery query, CancellationToken cancellationToken)
        => Task.FromResult<IReadOnlyList<SearchHit>>([]);
}
