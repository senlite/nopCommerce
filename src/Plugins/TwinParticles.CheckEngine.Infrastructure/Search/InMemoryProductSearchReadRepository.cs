using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TwinParticles.CheckEngine.Domain.Search;

namespace TwinParticles.CheckEngine.Infrastructure.Search;

public sealed class InMemoryProductSearchReadRepository : IProductSearchReadRepository
{
    private static readonly IReadOnlyList<SearchHit> Products =
    [
        new SearchHit { ProductId = 1001, Name = "BMW Oil Filter", CategoryId = 10, Brand = "BMW", Score = 0.95m },
        new SearchHit { ProductId = 1002, Name = "Radiator Hose", CategoryId = 20, Brand = "Conti", Score = 0.80m },
        new SearchHit { ProductId = 1003, Name = "Cabin Filter", CategoryId = 10, Brand = "Mann", Score = 0.75m }
    ];

    public Task<IReadOnlyList<SearchHit>> SearchKeywordAsync(SearchQuery query, CancellationToken cancellationToken)
    {
        var text = query.RawText?.Trim() ?? string.Empty;

        var hits = Products
            .Where(x => string.IsNullOrWhiteSpace(text) || x.Name.Contains(text, System.StringComparison.OrdinalIgnoreCase))
            .Select(Clone)
            .ToList();

        return Task.FromResult<IReadOnlyList<SearchHit>>(hits);
    }

    public Task<IReadOnlyList<SearchHit>> SearchByCategoryAsync(SearchQuery query, CancellationToken cancellationToken)
    {
        var hits = Products
            .Where(x => !query.Filters.CategoryId.HasValue || x.CategoryId == query.Filters.CategoryId)
            .Select(Clone)
            .ToList();

        return Task.FromResult<IReadOnlyList<SearchHit>>(hits);
    }

    public Task<IReadOnlyList<SearchHit>> SearchByVehicleTreeAsync(SearchQuery query, CancellationToken cancellationToken)
    {
        var hits = Products.Select(Clone).ToList();
        return Task.FromResult<IReadOnlyList<SearchHit>>(hits);
    }

    public Task<IReadOnlyList<SearchHit>> SearchByOemIdAsync(int oemNumberId, SearchQuery query, CancellationToken cancellationToken)
    {
        var hits = oemNumberId > 0 ? Products.Select(Clone).ToList() : [];
        return Task.FromResult<IReadOnlyList<SearchHit>>(hits);
    }

    private static SearchHit Clone(SearchHit hit)
    {
        return new SearchHit
        {
            ProductId = hit.ProductId,
            Name = hit.Name,
            CategoryId = hit.CategoryId,
            Brand = hit.Brand,
            Score = hit.Score
        };
    }
}
