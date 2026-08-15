using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LinqToDB.Data;
using Nop.Data;
using TwinParticles.CheckEngine.Domain.Search;

namespace TwinParticles.CheckEngine.Infrastructure.Search;

public sealed class SqlSearchEmbeddingCatalogSource : ISearchEmbeddingCatalogSource
{
    private readonly INopDataProvider _dataProvider;

    public SqlSearchEmbeddingCatalogSource(INopDataProvider dataProvider)
    {
        _dataProvider = dataProvider;
    }

    public async Task<IReadOnlyList<SearchEmbeddingDocument>> GetDocumentsAsync(string locale, CancellationToken cancellationToken)
    {
        var rows = await _dataProvider.QueryAsync<CatalogRow>(@"
SELECT ProductId, Name, NormalizedText, Price
FROM TP_CE_SearchIndex
ORDER BY ProductId");

        return rows.Select(row => new SearchEmbeddingDocument
        {
            ProductId = row.ProductId,
            Locale = locale,
            Name = row.Name,
            Text = $"{row.Name} {row.NormalizedText}",
            Price = row.Price
        }).ToList();
    }

    private sealed class CatalogRow
    {
        public int ProductId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string NormalizedText { get; set; } = string.Empty;
        public decimal Price { get; set; }
    }
}
