using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LinqToDB.Data;
using Nop.Data;
using TwinParticles.CheckEngine.Domain.Search;
using TwinParticles.CheckEngine.Infrastructure.Data;

namespace TwinParticles.CheckEngine.Infrastructure.Search;

public sealed class SqlSearchEmbeddingCatalogSource : ISearchEmbeddingCatalogSource
{
    private readonly INopDataProvider _dataProvider;
    private readonly BilingualSearchSynonymService _synonyms;

    public SqlSearchEmbeddingCatalogSource(
        INopDataProvider dataProvider,
        BilingualSearchSynonymService synonyms)
    {
        _dataProvider = dataProvider;
        _synonyms = synonyms;
    }

    public Task<IReadOnlyList<SearchEmbeddingDocument>> GetDocumentsAsync(string locale, CancellationToken cancellationToken) =>
        LoadDocumentsAsync(locale, staleOnly: false, options: null, cancellationToken);

    public Task<IReadOnlyList<SearchEmbeddingDocument>> GetStaleDocumentsAsync(
        string locale,
        SearchEmbeddingStaleOptions? options,
        CancellationToken cancellationToken) =>
        LoadDocumentsAsync(locale, staleOnly: true, options, cancellationToken);

    private async Task<IReadOnlyList<SearchEmbeddingDocument>> LoadDocumentsAsync(
        string locale,
        bool staleOnly,
        SearchEmbeddingStaleOptions? options,
        CancellationToken cancellationToken)
    {
        var normalizedLocale = string.IsNullOrWhiteSpace(locale) ? "en" : locale.Trim();
        var expectedModelHash = options?.ExpectedModelHash;
        var modelHashFilter = !string.IsNullOrWhiteSpace(expectedModelHash)
            ? " OR se.ModelHash <> @modelHash"
            : string.Empty;

        var staleFilter = staleOnly
            ? $@"
LEFT JOIN TP_CE_SearchEmbedding se ON se.ProductId = si.ProductId AND se.Locale = @locale
WHERE se.ProductId IS NULL OR si.UpdatedUtc > se.UpdatedUtc{modelHashFilter}"
            : string.Empty;

        var parameters = new List<DataParameter>
        {
            new("locale", normalizedLocale)
        };
        if (!string.IsNullOrWhiteSpace(expectedModelHash))
            parameters.Add(new DataParameter("modelHash", expectedModelHash));

        var rows = await _dataProvider.QueryAsync<CatalogRow>($@"
SELECT si.ProductId,
       COALESCE(lp.LocaleValue, si.Name) AS Name,
       si.NormalizedText,
       si.Sku,
       si.Mpn,
       si.Price,
       {CheckEngineSql.ScalarSubqueryLimitOne(
           "cat.Name",
           @"FROM Product_Category_Mapping pcm
INNER JOIN Category cat ON cat.Id = pcm.CategoryId AND cat.Deleted = 0 AND cat.Published = 1
WHERE pcm.ProductId = p.Id
ORDER BY pcm.DisplayOrder, cat.Id")} AS CategoryName,
       m.Name AS Brand
FROM TP_CE_SearchIndex si
INNER JOIN Product p ON p.Id = si.ProductId AND p.Deleted = 0 AND p.Published = 1
LEFT JOIN Language lang ON lang.UniqueSeoCode = @locale AND lang.Published = 1
LEFT JOIN LocalizedProperty lp ON lp.EntityId = p.Id
    AND lp.LocaleKeyGroup = N'Product'
    AND lp.LocaleKey = N'Name'
    AND lp.LanguageId = lang.Id
LEFT JOIN Manufacturer m ON m.Id = p.ManufacturerId AND m.Deleted = 0
{staleFilter}
ORDER BY si.ProductId",
            parameters.ToArray());

        return rows.Select(row => new SearchEmbeddingDocument
        {
            ProductId = row.ProductId,
            Locale = normalizedLocale,
            Name = row.Name,
            CategoryName = row.CategoryName,
            Brand = row.Brand,
            Price = row.Price,
            Text = _synonyms.Expand(
                SearchEmbeddingCatalogTextBuilder.Build(
                    row.Name,
                    row.CategoryName,
                    row.Brand,
                    row.Sku,
                    row.Mpn,
                    row.NormalizedText),
                normalizedLocale)
        }).ToList();
    }

    public async Task<int> GetCatalogCountAsync(string locale, CancellationToken cancellationToken)
    {
        var normalizedLocale = string.IsNullOrWhiteSpace(locale) ? "en" : locale.Trim();
        var count = await _dataProvider.QueryAsync<int>(@"
SELECT COUNT(*)
FROM TP_CE_SearchIndex si
INNER JOIN Product p ON p.Id = si.ProductId AND p.Deleted = 0 AND p.Published = 1",
            new DataParameter("locale", normalizedLocale));
        return count.FirstOrDefault();
    }

    public async Task<int> GetStaleCountAsync(string locale, SearchEmbeddingStaleOptions? options, CancellationToken cancellationToken)
    {
        var normalizedLocale = string.IsNullOrWhiteSpace(locale) ? "en" : locale.Trim();
        var expectedModelHash = options?.ExpectedModelHash;
        var modelHashFilter = !string.IsNullOrWhiteSpace(expectedModelHash)
            ? " OR se.ModelHash <> @modelHash"
            : string.Empty;

        var parameters = new List<DataParameter>
        {
            new("locale", normalizedLocale)
        };
        if (!string.IsNullOrWhiteSpace(expectedModelHash))
            parameters.Add(new DataParameter("modelHash", expectedModelHash));

        var count = await _dataProvider.QueryAsync<int>($@"
SELECT COUNT(*)
FROM TP_CE_SearchIndex si
INNER JOIN Product p ON p.Id = si.ProductId AND p.Deleted = 0 AND p.Published = 1
LEFT JOIN TP_CE_SearchEmbedding se ON se.ProductId = si.ProductId AND se.Locale = @locale
WHERE se.ProductId IS NULL OR si.UpdatedUtc > se.UpdatedUtc{modelHashFilter}",
            parameters.ToArray());
        return count.FirstOrDefault();
    }

    private sealed class CatalogRow
    {
        public int ProductId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string NormalizedText { get; set; } = string.Empty;
        public string? Sku { get; set; }
        public string? Mpn { get; set; }
        public decimal Price { get; set; }
        public string? CategoryName { get; set; }
        public string? Brand { get; set; }
    }
}
