using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using LinqToDB.Data;
using Nop.Data;
using TwinParticles.CheckEngine.Domain.Search;

namespace TwinParticles.CheckEngine.Infrastructure.Search;

public sealed class SqlSearchEmbeddingIndex : ISearchEmbeddingIndex
{
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
        await _dataProvider.ExecuteNonQueryAsync(@"
MERGE TP_CE_SearchEmbedding AS target
USING (SELECT @productId AS ProductId, @locale AS Locale) AS source
ON target.ProductId = source.ProductId AND target.Locale = source.Locale
WHEN MATCHED THEN UPDATE SET
    Name = @name,
    CategoryName = @categoryName,
    Brand = @brand,
    Price = @price,
    EmbeddingJson = @embeddingJson,
    ModelHash = @modelHash,
    UpdatedUtc = @updatedUtc
WHEN NOT MATCHED THEN INSERT
    (ProductId, Locale, Name, CategoryName, Brand, Price, EmbeddingJson, ModelHash, UpdatedUtc)
    VALUES (@productId, @locale, @name, @categoryName, @brand, @price, @embeddingJson, @modelHash, @updatedUtc);",
            new DataParameter("productId", document.ProductId),
            new DataParameter("locale", document.Locale),
            new DataParameter("name", document.Name),
            new DataParameter("categoryName", document.CategoryName ?? (object)DBNull.Value),
            new DataParameter("brand", document.Brand ?? (object)DBNull.Value),
            new DataParameter("price", document.Price ?? (object)DBNull.Value),
            new DataParameter("embeddingJson", embeddingJson),
            new DataParameter("modelHash", modelHash),
            new DataParameter("updatedUtc", DateTime.UtcNow));
    }

    public async Task<IReadOnlyList<SearchHit>> SearchSimilarAsync(
        float[] queryEmbedding,
        string locale,
        int take,
        CancellationToken cancellationToken)
    {
        var rows = await _dataProvider.QueryAsync<EmbeddingRow>(@"
SELECT ProductId, Name, CategoryName, Brand, Price, EmbeddingJson
FROM TP_CE_SearchEmbedding
WHERE Locale = @locale",
            new DataParameter("locale", locale));

        var hits = rows
            .Select(row =>
            {
                var vector = JsonSerializer.Deserialize<float[]>(row.EmbeddingJson) ?? [];
                return new SearchHit
                {
                    ProductId = row.ProductId,
                    Name = row.Name,
                    CategoryName = row.CategoryName,
                    Brand = row.Brand,
                    Price = row.Price,
                    Score = (decimal)VectorMath.CosineSimilarity(queryEmbedding, vector)
                };
            })
            .Where(hit => hit.Score > 0m)
            .OrderByDescending(hit => hit.Score)
            .ThenBy(hit => hit.ProductId)
            .Take(Math.Max(1, take))
            .ToList();

        return hits;
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
