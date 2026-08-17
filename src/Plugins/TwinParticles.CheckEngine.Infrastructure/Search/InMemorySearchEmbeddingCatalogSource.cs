using System;
using System.Collections.Generic;
using System.Linq;
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
            },
            new SearchEmbeddingDocument
            {
                ProductId = 1001,
                Locale = "ar",
                Name = "فلتر زيت BMW",
                CategoryName = "محرك",
                Brand = "BMW",
                Price = 24.90m,
                Text = "فلتر زيت BMW oil filter engine"
            },
            new SearchEmbeddingDocument
            {
                ProductId = 1002,
                Locale = "ar",
                Name = "خرطوم رديتر",
                CategoryName = "تبريد",
                Brand = "Conti",
                Price = 79.00m,
                Text = "خرطوم رديتر radiator hose cooling"
            },
            new SearchEmbeddingDocument
            {
                ProductId = 1003,
                Locale = "ar",
                Name = "فلتر مقصورة",
                CategoryName = "محرك",
                Brand = "Mann",
                Price = 18.50m,
                Text = "فلتر مقصورة cabin filter air"
            }
        ];
    }

    public Task<IReadOnlyList<SearchEmbeddingDocument>> GetDocumentsAsync(string locale, CancellationToken cancellationToken)
    {
        var filtered = string.IsNullOrWhiteSpace(locale)
            ? _documents
            : _documents.Where(document => string.Equals(document.Locale, locale, StringComparison.OrdinalIgnoreCase)).ToList();

        return Task.FromResult<IReadOnlyList<SearchEmbeddingDocument>>(filtered);
    }

    public Task<IReadOnlyList<SearchEmbeddingDocument>> GetStaleDocumentsAsync(
        string locale,
        SearchEmbeddingStaleOptions? options,
        CancellationToken cancellationToken) =>
        GetDocumentsAsync(locale, cancellationToken);

    public async Task<int> GetCatalogCountAsync(string locale, CancellationToken cancellationToken)
    {
        var documents = await GetDocumentsAsync(locale, cancellationToken);
        return documents.Count;
    }

    public async Task<int> GetStaleCountAsync(string locale, SearchEmbeddingStaleOptions? options, CancellationToken cancellationToken)
    {
        var documents = await GetStaleDocumentsAsync(locale, options, cancellationToken);
        return documents.Count;
    }
}
