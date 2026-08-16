using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using LinqToDB.Data;
using Nop.Data;
using TwinParticles.CheckEngine.Domain.Search;
using TwinParticles.CheckEngine.Infrastructure.Data;

namespace TwinParticles.CheckEngine.Infrastructure.Search;

public sealed class SqlSearchEmbeddingIndex : ISearchEmbeddingIndex
{
    private const int SearchPageSize = 512;

    private readonly INopDataProvider _dataProvider;

    public SqlSearchEmbeddingIndex(INopDataProvider dataProvider)
    {
        _dataProvider = dataProvider;
    }

    public async Task<bool> IsReadyAsync(string locale, CancellationToken cancellationToken)
    {
        var count = await _dataProvider.QueryAsync<int>(
            "SELECT COUNT(*) FROM TP_CE_SearchEmbedding WHERE Locale = @locale",
            new DataParameter("locale", locale));
        return count.FirstOrDefault() > 0;
    }

    public async Task UpsertAsync(SearchEmbeddingDocument document, float[] embedding, string modelHash, CancellationToken cancellationToken)
    {
        var embeddingJson = JsonSerializer.Serialize(embedding);
        var updatedUtc = DateTime.UtcNow;
        var updated = await _dataProvider.ExecuteNonQueryAsync(@"
UPDATE TP_CE_SearchEmbedding
SET Name = @name,
    CategoryName = @categoryName,
    Brand = @brand,
    Price = @price,
    EmbeddingJson = @embeddingJson,
    ModelHash = @modelHash,
    UpdatedUtc = @updatedUtc
WHERE ProductId = @productId AND Locale = @locale",
            new DataParameter("productId", document.ProductId),
            new DataParameter("locale", document.Locale),
            new DataParameter("name", document.Name),
            new DataParameter("categoryName", document.CategoryName ?? (object)DBNull.Value),
            new DataParameter("brand", document.Brand ?? (object)DBNull.Value),
            new DataParameter("price", document.Price ?? (object)DBNull.Value),
            new DataParameter("embeddingJson", embeddingJson),
            new DataParameter("modelHash", modelHash),
            new DataParameter("updatedUtc", updatedUtc));

        if (updated > 0)
            return;

        await _dataProvider.ExecuteNonQueryAsync(@"
INSERT INTO TP_CE_SearchEmbedding
    (ProductId, Locale, Name, CategoryName, Brand, Price, EmbeddingJson, ModelHash, UpdatedUtc)
VALUES (@productId, @locale, @name, @categoryName, @brand, @price, @embeddingJson, @modelHash, @updatedUtc)",
            new DataParameter("productId", document.ProductId),
            new DataParameter("locale", document.Locale),
            new DataParameter("name", document.Name),
            new DataParameter("categoryName", document.CategoryName ?? (object)DBNull.Value),
            new DataParameter("brand", document.Brand ?? (object)DBNull.Value),
            new DataParameter("price", document.Price ?? (object)DBNull.Value),
            new DataParameter("embeddingJson", embeddingJson),
            new DataParameter("modelHash", modelHash),
            new DataParameter("updatedUtc", updatedUtc));
    }

    public async Task<IReadOnlyList<SearchHit>> SearchSimilarAsync(
        float[] queryEmbedding,
        string locale,
        int take,
        CancellationToken cancellationToken)
    {
        var topK = new CosineSimilarityTopK<SearchHit>(queryEmbedding, Math.Max(1, take), hit => hit.ProductId);
        var offset = 0;

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var rows = await _dataProvider.QueryAsync<EmbeddingRow>(@"
SELECT ProductId, Name, CategoryName, Brand, Price, EmbeddingJson
FROM TP_CE_SearchEmbedding
WHERE Locale = @locale
ORDER BY ProductId
OFFSET @offset ROWS FETCH NEXT @pageSize ROWS ONLY",
                new DataParameter("locale", locale),
                new DataParameter("offset", offset),
                new DataParameter("pageSize", SearchPageSize));

            var page = rows.ToList();
            if (page.Count == 0)
                break;

            foreach (var row in page)
            {
                var vector = JsonSerializer.Deserialize<float[]>(row.EmbeddingJson) ?? [];
                topK.Consider(new SearchHit
                {
                    ProductId = row.ProductId,
                    Name = row.Name,
                    CategoryName = row.CategoryName,
                    Brand = row.Brand,
                    Price = row.Price
                }, vector);
            }

            if (page.Count < SearchPageSize)
                break;

            offset += SearchPageSize;
        }

        return topK.Results()
            .Select(entry => new SearchHit
            {
                ProductId = entry.Item.ProductId,
                Name = entry.Item.Name,
                CategoryName = entry.Item.CategoryName,
                Brand = entry.Item.Brand,
                Price = entry.Item.Price,
                Score = (decimal)entry.Score
            })
            .ToList();
    }

    public Task ClearAsync(string locale, CancellationToken cancellationToken)
    {
        return _dataProvider.ExecuteNonQueryAsync(
            "DELETE FROM TP_CE_SearchEmbedding WHERE Locale = @locale",
            new DataParameter("locale", locale));
    }

    public async Task<int> GetCountAsync(string locale, CancellationToken cancellationToken)
    {
        var count = await _dataProvider.QueryAsync<int>(
            "SELECT COUNT(*) FROM TP_CE_SearchEmbedding WHERE Locale = @locale",
            new DataParameter("locale", locale));
        return count.FirstOrDefault();
    }

    private sealed class EmbeddingRow
    {
        public int ProductId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? CategoryName { get; set; }
        public string? Brand { get; set; }
        public decimal? Price { get; set; }
        public string EmbeddingJson { get; set; } = string.Empty;
    }
}
