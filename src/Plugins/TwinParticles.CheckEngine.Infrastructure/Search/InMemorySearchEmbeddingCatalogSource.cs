using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TwinParticles.CheckEngine.Domain.Search;

namespace TwinParticles.CheckEngine.Infrastructure.Search;

public sealed class InMemorySearchEmbeddingCatalogSource : ISearchEmbeddingCatalogSource
{
    private readonly IReadOnlyList<SearchEmbeddingDocument> _documents;

    public InMemorySearchEmbeddingCatalogSource(IReadOnlyList<SearchEmbeddingDocument>? documents = null)
    {
        _documents = documents ??
        [
            new SearchEmbeddingDocument
            {
                ProductId = 1001,
                Locale = "en",
                Name = "BMW Oil Filter",
                CategoryName = "Engine",
                Brand = "BMW",
                Price = 24.90m,
                Text = "BMW Oil Filter engine bmw oil filter"
            },
            new SearchEmbeddingDocument
            {
                ProductId = 1002,
                Locale = "en",
                Name = "Radiator Hose",
                CategoryName = "Cooling",
                Brand = "Conti",
                Price = 79.00m,
                Text = "Radiator Hose cooling radiator hose"
            },
            new SearchEmbeddingDocument
            {
                ProductId = 1003,
                Locale = "en",
                Name = "Cabin Filter",
                CategoryName = "Engine",
                Brand = "Mann",
                Price = 18.50m,
                Text = "Cabin Filter engine cabin filter air"
            }
        ];
    }

    public Task<IReadOnlyList<SearchEmbeddingDocument>> GetDocumentsAsync(string locale, CancellationToken cancellationToken)
    {
        return Task.FromResult(_documents);
    }
}
