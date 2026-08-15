using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TwinParticles.CheckEngine.Domain.Search;

namespace TwinParticles.CheckEngine.Infrastructure.Search;

public sealed class InMemorySearchEmbeddingIndex : ISearchEmbeddingIndex
{
    private sealed class Entry
    {
        public SearchEmbeddingDocument Document { get; init; } = new();
        public float[] Embedding { get; init; } = [];
        public string ModelHash { get; init; } = string.Empty;
    }

    private readonly ConcurrentDictionary<string, Entry> _entries = new(StringComparer.OrdinalIgnoreCase);

    public Task<bool> IsReadyAsync(string locale, CancellationToken cancellationToken)
    {
        var ready = _entries.Keys.Any(key => key.EndsWith($"|{locale}", StringComparison.OrdinalIgnoreCase));
        return Task.FromResult(ready);
    }

    public Task UpsertAsync(SearchEmbeddingDocument document, float[] embedding, string modelHash, CancellationToken cancellationToken)
    {
        var key = BuildKey(document.ProductId, document.Locale);
        _entries[key] = new Entry
        {
            Document = document,
            Embedding = embedding,
            ModelHash = modelHash
        };
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<SearchHit>> SearchSimilarAsync(
        float[] queryEmbedding,
        string locale,
        int take,
        CancellationToken cancellationToken)
    {
        var hits = _entries.Values
            .Where(entry => string.Equals(entry.Document.Locale, locale, StringComparison.OrdinalIgnoreCase))
            .Select(entry => new SearchHit
            {
                ProductId = entry.Document.ProductId,
                Name = entry.Document.Name,
                CategoryName = entry.Document.CategoryName,
                Brand = entry.Document.Brand,
                Price = entry.Document.Price,
                Score = (decimal)VectorMath.CosineSimilarity(queryEmbedding, entry.Embedding)
            })
            .Where(hit => hit.Score > 0m)
            .OrderByDescending(hit => hit.Score)
            .ThenBy(hit => hit.ProductId)
            .Take(Math.Max(1, take))
            .ToList();

        return Task.FromResult<IReadOnlyList<SearchHit>>(hits);
    }

    public Task ClearAsync(string locale, CancellationToken cancellationToken)
    {
        foreach (var key in _entries.Keys.Where(key => key.EndsWith($"|{locale}", StringComparison.OrdinalIgnoreCase)).ToList())
            _entries.TryRemove(key, out _);

        return Task.CompletedTask;
    }

    public Task<int> GetCountAsync(string locale, CancellationToken cancellationToken)
    {
        var count = _entries.Keys.Count(key => key.EndsWith($"|{locale}", StringComparison.OrdinalIgnoreCase));
        return Task.FromResult(count);
    }

    private static string BuildKey(int productId, string locale) => $"{productId}|{locale}";
}
