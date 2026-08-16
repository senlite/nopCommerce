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
using TwinParticles.CheckEngine.Infrastructure.Data;

namespace TwinParticles.CheckEngine.Infrastructure.Search;

public sealed class SqlProductSearchReadRepository : IProductSearchReadRepository
{
    private readonly INopDataProvider _dataProvider;
    private readonly ISearchIndexHealthService _healthService;
    private readonly ISearchIndexStateReader? _indexStateReader;
    private readonly IProductService _productService;
    private readonly IStoreContext _storeContext;
    private readonly IWorkContext _workContext;

    public SqlProductSearchReadRepository(
        INopDataProvider dataProvider,
        IProductService productService,
        IStoreContext storeContext,
        IWorkContext workContext,
        ISearchIndexHealthService healthService,
        ISearchIndexStateReader? indexStateReader = null)
    {
        _dataProvider = dataProvider;
        _productService = productService;
        _storeContext = storeContext;
        _workContext = workContext;
        _healthService = healthService;
        _indexStateReader = indexStateReader;
    }

    public async Task<IReadOnlyList<SearchHit>> SearchKeywordAsync(SearchQuery query, CancellationToken cancellationToken)
    {
        var text = query.RawText?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(text))
            return [];

        // Prefer the incremental keyword projection once it has been built; otherwise, and on any
        // projection failure, degrade to the authoritative live catalog (FR-446, ADR-014).
        if (query.Filters.CategoryId is null or <= 0 && await IsProjectionReadyAsync(cancellationToken))
        {
            var projected = await SearchProjectionAsync(query, text, cancellationToken);
            if (projected.Count > 0)
                return projected;
        }

        return await SearchNopCatalogAsync(query, text, cancellationToken);
    }

    private async Task<bool> IsProjectionReadyAsync(CancellationToken cancellationToken)
    {
        if (_indexStateReader is null)
            return false;

        try
        {
            return (await _indexStateReader.GetStateAsync(cancellationToken)).IsReady;
        }
        catch
        {
            return false;
        }
    }

    private async Task<IReadOnlyList<SearchHit>> SearchProjectionAsync(
        SearchQuery query,
        string text,
        CancellationToken cancellationToken)
    {
        try
        {
            var normalized = text.ToLowerInvariant();
            var like = "%" + EscapeLike(normalized) + "%";

            var rows = await _dataProvider.QueryAsync<ProjectionRow>(
                CheckEngineSql.SelectTop(
                    5000,
                    @"ProductId,
    CASE WHEN Sku = @raw OR Mpn = @raw THEN CAST(1.0 AS decimal(5,4))
         WHEN NormalizedText LIKE @exactWord ESCAPE '\' THEN CAST(0.9 AS decimal(5,4))
         ELSE CAST(0.75 AS decimal(5,4)) END AS Score",
                    @"FROM TP_CE_SearchIndex
WHERE NormalizedText LIKE @like ESCAPE '\' OR Sku = @raw OR Mpn = @raw
ORDER BY Score DESC, ProductId"),
                new DataParameter("like", like),
                new DataParameter("exactWord", "%" + EscapeLike(normalized) + "%"),
                new DataParameter("raw", text));

            var priceMin = query.Filters.PriceMin;
            var priceMax = query.Filters.PriceMax;
            var candidates = rows.Select(row => (row.ProductId, row.Score)).ToList();
            var hits = await HydrateProductsAsync(candidates, cancellationToken);

            if (priceMin.HasValue || priceMax.HasValue)
            {
                hits = hits
                    .Where(hit => (!priceMin.HasValue || hit.Price >= priceMin.Value)
                                  && (!priceMax.HasValue || hit.Price <= priceMax.Value))
                    .ToList();
            }

            return hits;
        }
        catch (Exception exception)
        {
            await _healthService.ReportDegradedAsync(exception.GetType().Name, cancellationToken);
            return [];
        }
    }

    private static string EscapeLike(string value)
        => value.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_").Replace("[", "\\[");

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
                cancellationToken);
        }
        catch (Exception exception)
        {
            await _healthService.ReportDegradedAsync(exception.GetType().Name, cancellationToken);
            return [];
        }
    }

    public async Task<IReadOnlyList<SearchHit>> SuggestProductsAsync(string prefix, string locale, int take, CancellationToken cancellationToken)
    {
        var text = prefix?.Trim() ?? string.Empty;
        if (text.Length < 2)
            return [];

        var limit = take <= 0 ? 5 : Math.Min(take, 10);

        try
        {
            var store = await _storeContext.GetCurrentStoreAsync();
            var language = await _workContext.GetWorkingLanguageAsync();

            var products = await _productService.SearchProductsAsync(
                pageIndex: 0,
                pageSize: limit,
                storeId: store.Id,
                visibleIndividuallyOnly: true,
                keywords: text,
                searchDescriptions: false,
                searchManufacturerPartNumber: true,
                searchSku: true,
                languageId: language.Id,
                showHidden: false,
                overridePublished: true);

            return products
                .Where(product => product.Published && !product.Deleted && product.VisibleIndividually)
                .Take(limit)
                .Select(product => new SearchHit
                {
                    ProductId = product.Id,
                    Name = product.Name,
                    Price = product.Price,
                    Score = 0.5m
                })
                .ToList();
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

            var hits = products
                .Where(product => product.Published && !product.Deleted && product.VisibleIndividually)
                .Select(product => MapProduct(product, keywords))
                .ToList();

            await EnrichAsync(hits, cancellationToken);
            return hits;
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
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (candidates.Count == 0)
            return [];

        var scoreById = candidates
            .GroupBy(candidate => candidate.ProductId)
            .ToDictionary(group => group.Key, group => group.Max(candidate => candidate.Score));
        var products = await _productService.GetProductsByIdsAsync(scoreById.Keys.ToArray());

        var hits = products
            .Where(product => product.Published && !product.Deleted && product.VisibleIndividually)
            .Select(product => new SearchHit
            {
                ProductId = product.Id,
                Name = product.Name,
                Price = product.Price,
                Score = scoreById[product.Id]
            })
            .OrderByDescending(hit => hit.Score)
            .ThenBy(hit => hit.ProductId)
            .ToList();

        await EnrichAsync(hits, cancellationToken);
        return hits;
    }

    // Populates category, category name, and brand from nopCommerce mapping tables in two batched
    // queries so facets reflect real catalog metadata without an N+1 per hit. Enrichment failures are
    // non-fatal: the hits still return (facets simply carry less dimension) and health degrades.
    private async Task EnrichAsync(List<SearchHit> hits, CancellationToken cancellationToken)
    {
        if (hits.Count == 0)
            return;

        var idList = string.Join(",", hits.Select(hit => hit.ProductId).Distinct());

        try
        {
            var categoryRows = await _dataProvider.QueryAsync<ProductCategoryRow>($@"
SELECT m.ProductId, m.CategoryId, c.Name AS CategoryName, m.DisplayOrder
FROM Product_Category_Mapping m
INNER JOIN Category c ON c.Id = m.CategoryId
WHERE m.ProductId IN ({idList})");

            var categoryByProduct = categoryRows
                .GroupBy(row => row.ProductId)
                .ToDictionary(group => group.Key, group => group.OrderBy(row => row.DisplayOrder).First());

            var manufacturerRows = await _dataProvider.QueryAsync<ProductManufacturerRow>($@"
SELECT m.ProductId, man.Name AS ManufacturerName, m.DisplayOrder
FROM Product_Manufacturer_Mapping m
INNER JOIN Manufacturer man ON man.Id = m.ManufacturerId
WHERE m.ProductId IN ({idList})");

            var brandByProduct = manufacturerRows
                .GroupBy(row => row.ProductId)
                .ToDictionary(group => group.Key, group => group.OrderBy(row => row.DisplayOrder).First().ManufacturerName);

            foreach (var hit in hits)
            {
                if (categoryByProduct.TryGetValue(hit.ProductId, out var category))
                {
                    hit.CategoryId = category.CategoryId;
                    hit.CategoryName = category.CategoryName;
                }

                if (brandByProduct.TryGetValue(hit.ProductId, out var brand))
                    hit.Brand = brand;
            }
        }
        catch (Exception exception)
        {
            await _healthService.ReportDegradedAsync(exception.GetType().Name, cancellationToken);
        }
    }

    private static SearchHit MapProduct(Product product, string? keywords)
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
            Price = product.Price,
            Score = score
        };
    }

    private sealed class ProductIdRow
    {
        public int ProductId { get; set; }
    }

    private sealed class ProjectionRow
    {
        public int ProductId { get; set; }

        public decimal Score { get; set; }
    }

    private sealed class OemMapRow
    {
        public int ProductId { get; set; }

        public bool IsPrimary { get; set; }
    }

    private sealed class ProductCategoryRow
    {
        public int ProductId { get; set; }

        public int CategoryId { get; set; }

        public string CategoryName { get; set; } = string.Empty;

        public int DisplayOrder { get; set; }
    }

    private sealed class ProductManufacturerRow
    {
        public int ProductId { get; set; }

        public string ManufacturerName { get; set; } = string.Empty;

        public int DisplayOrder { get; set; }
    }
}
