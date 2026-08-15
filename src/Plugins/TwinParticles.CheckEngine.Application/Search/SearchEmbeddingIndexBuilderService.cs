using System.Threading;
using System.Threading.Tasks;
using TwinParticles.CheckEngine.Domain.Ai;
using TwinParticles.CheckEngine.Domain.Search;

namespace TwinParticles.CheckEngine.Application.Search;

public sealed class SearchEmbeddingIndexBuilderService
{
    private readonly ISearchEmbeddingCatalogSource _catalogSource;
    private readonly IAiEmbeddingPort _embeddingPort;
    private readonly ISearchEmbeddingIndex _embeddingIndex;

    public SearchEmbeddingIndexBuilderService(
        ISearchEmbeddingCatalogSource catalogSource,
        ISearchEmbeddingIndex embeddingIndex,
        IAiEmbeddingPort embeddingPort)
    {
        _catalogSource = catalogSource;
        _embeddingIndex = embeddingIndex;
        _embeddingPort = embeddingPort;
    }

    public async Task<int> RebuildAsync(string locale, CancellationToken cancellationToken)
    {
        await _embeddingIndex.ClearAsync(locale, cancellationToken);
        var documents = await _catalogSource.GetDocumentsAsync(locale, cancellationToken);
        var indexed = 0;

        foreach (var document in documents)
        {
            var embedding = await _embeddingPort.EmbedAsync(new AiEmbeddingRequest
            {
                FeatureKey = AiFeatureKeys.SearchSemantic,
                Text = document.Text,
                Locale = locale
            }, cancellationToken);

            if (!embedding.Success || embedding.Vector.Length == 0)
                continue;

            await _embeddingIndex.UpsertAsync(document, embedding.Vector, embedding.ModelHash, cancellationToken);
            indexed++;
        }

        return indexed;
    }
}
