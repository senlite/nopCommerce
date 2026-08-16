using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace TwinParticles.CheckEngine.Domain.Search;

public interface ISearchEmbeddingCatalogSource
{
    Task<IReadOnlyList<SearchEmbeddingDocument>> GetDocumentsAsync(string locale, CancellationToken cancellationToken);

    /// <summary>
    /// Returns catalog documents that are missing embeddings or stale relative to the keyword projection.
    /// </summary>
    Task<IReadOnlyList<SearchEmbeddingDocument>> GetStaleDocumentsAsync(
        string locale,
        SearchEmbeddingStaleOptions? options,
        CancellationToken cancellationToken);

    Task<int> GetCatalogCountAsync(string locale, CancellationToken cancellationToken);

    Task<int> GetStaleCountAsync(string locale, SearchEmbeddingStaleOptions? options, CancellationToken cancellationToken);
}
