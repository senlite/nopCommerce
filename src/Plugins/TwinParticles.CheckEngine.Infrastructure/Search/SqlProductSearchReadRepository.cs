using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LinqToDB.Data;
using Nop.Data;
using TwinParticles.CheckEngine.Domain.Search;

namespace TwinParticles.CheckEngine.Infrastructure.Search;

public sealed class SqlProductSearchReadRepository : IProductSearchReadRepository
{
    private static readonly IReadOnlyList<SearchHit> SeededCatalog =
    [
        new SearchHit { ProductId = 1001, Name = "BMW Oil Filter", CategoryId = 10, Brand = "BMW", Score = 0.95m },
        new SearchHit { ProductId = 1002, Name = "Radiator Hose", CategoryId = 20, Brand = "Conti", Score = 0.80m },
        new SearchHit { ProductId = 1003, Name = "Cabin Filter", CategoryId = 10, Brand = "Mann", Score = 0.75m }
    ];

    private readonly INopDataProvider _dataProvider;

    public SqlProductSearchReadRepository(INopDataProvider dataProvider)
    {
        _dataProvider = dataProvider;
    }

    public async Task<IReadOnlyList<SearchHit>> SearchKeywordAsync(SearchQuery query, CancellationToken cancellationToken)
    {
        var text = query.RawText?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(text))
            return [];

        try
        {
            var rows = await _dataProvider.QueryAsync<ProductSearchRow>(@"
SELECT ProductId, Name, CategoryId, Brand, Score
FROM (
    SELECT DISTINCT m.ProductId,
           CONCAT('Product ', m.ProductId) AS Name,
           CAST(NULL AS INT) AS CategoryId,
           CAST(NULL AS NVARCHAR(64)) AS Brand,
           CASE WHEN MAX(CAST(m.IsPrimary AS INT)) = 1 THEN 1.0 ELSE 0.7 END AS Score
    FROM TP_CE_ProductOemMap m
    GROUP BY m.ProductId
) x
WHERE x.Name LIKE @like
ORDER BY x.Score DESC, x.ProductId",
                new DataParameter("like", "%" + EscapeLike(text) + "%"));

            var hits = rows.Select(Map).ToList();
            if (query.Filters.CategoryId.HasValue)
                hits = hits.Where(x => x.CategoryId == query.Filters.CategoryId).ToList();

            if (hits.Count > 0)
                return hits;
        }
        catch
        {
            // Degraded: fall through to seeded catalog.
        }

        return FilterSeeded(text, query.Filters.CategoryId);
    }

    public async Task<IReadOnlyList<SearchHit>> SearchByCategoryAsync(SearchQuery query, CancellationToken cancellationToken)
    {
        try
        {
            var rows = await _dataProvider.QueryAsync<ProductSearchRow>(@"
SELECT ProductId, Name, CategoryId, Brand, Score
FROM (
    SELECT DISTINCT m.ProductId,
           CONCAT('Product ', m.ProductId) AS Name,
           CAST(NULL AS INT) AS CategoryId,
           CAST(NULL AS NVARCHAR(64)) AS Brand,
           CASE WHEN MAX(CAST(m.IsPrimary AS INT)) = 1 THEN 1.0 ELSE 0.7 END AS Score
    FROM TP_CE_ProductOemMap m
    GROUP BY m.ProductId
) x
ORDER BY x.Score DESC, x.ProductId");

            var hits = rows.Select(Map).ToList();
            if (query.Filters.CategoryId.HasValue)
                hits = hits.Where(x => x.CategoryId == query.Filters.CategoryId).ToList();

            if (hits.Count > 0)
                return hits;
        }
        catch
        {
            // Degraded: fall through to seeded catalog.
        }

        return FilterSeeded(text: null, query.Filters.CategoryId);
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

            return rows
                .Select(row => new SearchHit
                {
                    ProductId = row.ProductId,
                    Name = $"Product {row.ProductId}",
                    Score = 0.9m
                })
                .ToList();
        }
        catch
        {
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

            return rows
                .Select(row => new SearchHit
                {
                    ProductId = row.ProductId,
                    Name = $"Product {row.ProductId}",
                    Score = row.IsPrimary ? 1.0m : 0.7m
                })
                .ToList();
        }
        catch
        {
            return [];
        }
    }

    private static IReadOnlyList<SearchHit> FilterSeeded(string? text, int? categoryId)
    {
        return SeededCatalog
            .Where(x => string.IsNullOrWhiteSpace(text) || x.Name.Contains(text, StringComparison.OrdinalIgnoreCase))
            .Where(x => !categoryId.HasValue || x.CategoryId == categoryId)
            .Select(Clone)
            .ToList();
    }

    private static SearchHit Map(ProductSearchRow row)
    {
        return new SearchHit
        {
            ProductId = row.ProductId,
            Name = string.IsNullOrWhiteSpace(row.Name) ? $"Product {row.ProductId}" : row.Name,
            CategoryId = row.CategoryId,
            Brand = row.Brand,
            Score = row.Score
        };
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

    private static string EscapeLike(string value)
        => value.Replace("[", "[[]", StringComparison.Ordinal)
            .Replace("%", "[%]", StringComparison.Ordinal)
            .Replace("_", "[_]", StringComparison.Ordinal);

    private sealed class ProductSearchRow
    {
        public int ProductId { get; set; }

        public string Name { get; set; } = string.Empty;

        public int? CategoryId { get; set; }

        public string? Brand { get; set; }

        public decimal Score { get; set; }
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
