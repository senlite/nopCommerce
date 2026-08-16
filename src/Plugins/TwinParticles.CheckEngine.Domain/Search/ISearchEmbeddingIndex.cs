using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace TwinParticles.CheckEngine.Domain.Search;

public interface ISearchEmbeddingIndex
{
    Task<bool> IsReadyAsync(string locale, CancellationToken cancellationToken);

    Task UpsertAsync(SearchEmbeddingDocument document, float[] embedding, string modelHash, CancellationToken cancellationToken);

    Task<IReadOnlyList<SearchHit>> SearchSimilarAsync(
        float[] queryEmbedding,
        string locale,
        int take,
        CancellationToken cancellationToken);

    Task ClearAsync(string locale, CancellationToken cancellationToken);

    Task<int> GetCountAsync(string locale, CancellationToken cancellationToken);
}
