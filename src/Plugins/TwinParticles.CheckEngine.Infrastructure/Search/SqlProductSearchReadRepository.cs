using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LinqToDB.Data;
using Nop.Core;
using Nop.Core.Domain.Catalog;
using Nop.Data;
using Nop.Services.Catalog;
using TwinParticles.CheckEngine.Domain.Search;

namespace TwinParticles.CheckEngine.Infrastructure.Search;

public sealed class SqlProductSearchReadRepository : IProductSearchReadRepository
{
    private readonly INopDataProvider _dataProvider;
    private readonly ISearchIndexHealthService _healthService;
    private readonly IProductService _productService;
    private readonly IStoreContext _storeContext;
    private readonly IWorkContext _workContext;

    public SqlProductSearchReadRepository(
        INopDataProvider dataProvider,
        IProductService productService,
        IStoreContext storeContext,
        IWorkContext workContext,
        ISearchIndexHealthService healthService)
    {
        _dataProvider = dataProvider;
        _productService = productService;
        _storeContext = storeContext;
        _workContext = workContext;
        _healthService = healthService;
    }

    public async Task<IReadOnlyList<SearchHit>> SearchKeywordAsync(SearchQuery query, CancellationToken cancellationToken)
    {
        var text = query.RawText?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(text))
            return [];

        return await SearchNopCatalogAsync(query, text, cancellationToken);
    }

    public async Task<IReadOnlyList<SearchHit>> SearchByCategoryAsync(SearchQuery query, CancellationToken cancellationToken)
    {
        if (!query.Filters.CategoryId.HasValue || query.Filters.CategoryId.Value <= 0)
            return [];

        return await SearchNopCatalogAsync(query, keywords: null, cancellationToken);
    }

    public async Task<IReadOnlyList<SearchHit>> SearchByVehicleTreeAsync(SearchQuery query, CancellationToken cancellationToken)
    {
        if (!query.VehicleConfigurationId.HasValue || query.VehicleConfigurationId.Value <= 0)
            return [];

        try
        {
            var rows = await _dataProvider.QueryAsync<ProductIdRow>(@"
SELECT DISTINCT ProductId
FROM TP_CE_FitmentClaim
WHERE VehicleConfigurationId = @vehicleConfigurationId
  AND IsPublished = 1
  AND FitmentStatusId = 1
ORDER BY ProductId",
                new DataParameter("vehicleConfigurationId", query.VehicleConfigurationId.Value));

            return await HydrateProductsAsync(
                rows.Select(row => (row.ProductId, Score: 0.9m)).ToList(),
                query.Filters.CategoryId,
                cancellationToken);
        }
        catch (Exception exception)
        {
            await _healthService.ReportDegradedAsync(exception.GetType().Name, cancellationToken);
            return [];
        }
    }

    public async Task<IReadOnlyList<SearchHit>> SearchByOemIdAsync(int oemNumberId, SearchQuery query, CancellationToken cancellationToken)
    {
        if (oemNumberId <= 0)
            return [];

        try
        {
            var rows = await _dataProvider.QueryAsync<OemMapRow>(@"
SELECT ProductId, IsPrimary
FROM TP_CE_ProductOemMap
WHERE OemNumberId = @oemNumberId
ORDER BY IsPrimary DESC, ProductId",
                new DataParameter("oemNumberId", oemNumberId));

            return await HydrateProductsAsync(
                rows.Select(row => (row.ProductId, Score: row.IsPrimary ? 1.0m : 0.7m)).ToList(),
                query.Filters.CategoryId,
                cancellationToken);
        }
        catch (Exception exception)
        {
            await _healthService.ReportDegradedAsync(exception.GetType().Name, cancellationToken);
            return [];
        }
    }

    private async Task<IReadOnlyList<SearchHit>> SearchNopCatalogAsync(
        SearchQuery query,
        string? keywords,
        CancellationToken cancellationToken)
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            var store = await _storeContext.GetCurrentStoreAsync();
            var language = await _workContext.GetWorkingLanguageAsync();
            var categories = query.Filters.CategoryId.HasValue
                ? new List<int> { query.Filters.CategoryId.Value }
                : null;

            // The application layer applies fitment and final paging after this projection. Cap the
            // candidate set to prevent an unbounded allocation while leaving enough room for the
            // fitment filter to remove non-matching products.
            var products = await _productService.SearchProductsAsync(
                pageIndex: 0,
                pageSize: 5000,
                categoryIds: categories,
                storeId: store.Id,
                visibleIndividuallyOnly: true,
                priceMin: query.Filters.PriceMin,
                priceMax: query.Filters.PriceMax,
                keywords: keywords,
                searchDescriptions: true,
                searchManufacturerPartNumber: true,
                searchSku: true,
                languageId: language.Id,
                showHidden: false,
                overridePublished: true);

            return products
                .Where(product => product.Published && !product.Deleted && product.VisibleIndividually)
                .Select(product => MapProduct(product, query.Filters.CategoryId, keywords))
                .ToList();
        }
        catch (Exception exception)
        {
            // Honest degradation: return no invented products and let UnifiedSearchService expose
            // IsDegraded. The previous fallback returned synthetic IDs 1001-1003 that did not exist
            // in the nopCommerce catalog.
            await _healthService.ReportDegradedAsync(exception.GetType().Name, cancellationToken);
            return [];
        }
    }

    private async Task<IReadOnlyList<SearchHit>> HydrateProductsAsync(
        IReadOnlyList<(int ProductId, decimal Score)> candidates,
        int? categoryId,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (candidates.Count == 0)
            return [];

        var scoreById = candidates
            .GroupBy(candidate => candidate.ProductId)
            .ToDictionary(group => group.Key, group => group.Max(candidate => candidate.Score));
        var products = await _productService.GetProductsByIdsAsync(scoreById.Keys.ToArray());

        return products
            .Where(product => product.Published && !product.Deleted && product.VisibleIndividually)
            .Select(product => new SearchHit
            {
                ProductId = product.Id,
                Name = product.Name,
                CategoryId = categoryId,
                Brand = null,
                Score = scoreById[product.Id]
            })
            .OrderByDescending(hit => hit.Score)
            .ThenBy(hit => hit.ProductId)
            .ToList();
    }

    private static SearchHit MapProduct(Product product, int? categoryId, string? keywords)
    {
        var score = 0.75m;
        if (!string.IsNullOrWhiteSpace(keywords))
        {
            if (string.Equals(product.Sku, keywords, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(product.ManufacturerPartNumber, keywords, StringComparison.OrdinalIgnoreCase))
                score = 1.0m;
            else if (product.Name.Contains(keywords, StringComparison.OrdinalIgnoreCase))
                score = 0.9m;
        }

        return new SearchHit
        {
            ProductId = product.Id,
            Name = product.Name,
            CategoryId = categoryId,
            Brand = null,
            Score = score
        };
    }

    private sealed class ProductIdRow
    {
        public int ProductId { get; set; }
    }

    private sealed class OemMapRow
    {
        public int ProductId { get; set; }

        public bool IsPrimary { get; set; }
    }
}
