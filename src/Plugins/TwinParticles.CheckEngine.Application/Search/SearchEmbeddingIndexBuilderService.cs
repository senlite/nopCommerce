using System.Linq;
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

    public async Task<SearchEmbeddingRebuildResult> RebuildAsync(string locale, CancellationToken cancellationToken)
    {
        await _embeddingIndex.ClearAsync(locale, cancellationToken);
        var documents = await _catalogSource.GetDocumentsAsync(locale, cancellationToken);
        return await IndexDocumentsAsync(locale, documents, cancellationToken);
    }

    public async Task<SearchEmbeddingRebuildResult> RefreshIncrementalAsync(string locale, CancellationToken cancellationToken)
    {
        var staleOptions = await CreateStaleOptionsAsync(cancellationToken);
        var documents = await _catalogSource.GetStaleDocumentsAsync(locale, staleOptions, cancellationToken);
        return await IndexDocumentsAsync(locale, documents, cancellationToken);
    }

    public async Task<SearchEmbeddingStaleOptions?> CreateStaleOptionsAsync(CancellationToken cancellationToken)
    {
        var modelHash = await ResolveCurrentModelHashAsync(cancellationToken);
        return string.IsNullOrWhiteSpace(modelHash)
            ? null
            : new SearchEmbeddingStaleOptions { ExpectedModelHash = modelHash };
    }

    private async Task<string?> ResolveCurrentModelHashAsync(CancellationToken cancellationToken)
    {
        var probe = await _embeddingPort.EmbedAsync(new AiEmbeddingRequest
        {
            FeatureKey = AiFeatureKeys.SearchSemantic,
            Text = "catalog",
            Locale = "en"
        }, cancellationToken);

        return probe.Success && !string.IsNullOrWhiteSpace(probe.ModelHash)
            ? probe.ModelHash
            : null;
    }

    private async Task<SearchEmbeddingRebuildResult> IndexDocumentsAsync(
        string locale,
        IReadOnlyList<SearchEmbeddingDocument> documents,
        CancellationToken cancellationToken)
    {
        var indexed = 0;
        var skipped = 0;
        var failed = 0;
        const int batchSize = 32;

        for (var offset = 0; offset < documents.Count; offset += batchSize)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var batch = documents.Skip(offset).Take(batchSize).ToList();

            foreach (var document in batch)
            {
                var embedding = await _embeddingPort.EmbedAsync(new AiEmbeddingRequest
                {
                    FeatureKey = AiFeatureKeys.SearchSemantic,
                    Text = document.Text,
                    Locale = locale
                }, cancellationToken);

                if (!embedding.Success)
                {
                    failed++;
                    continue;
                }

                if (embedding.Vector.Length == 0)
                {
                    skipped++;
                    continue;
                }

                await _embeddingIndex.UpsertAsync(document, embedding.Vector, embedding.ModelHash, cancellationToken);
                indexed++;
            }
        }

        return new SearchEmbeddingRebuildResult
        {
            Locale = locale,
            CatalogCount = documents.Count,
            Indexed = indexed,
            Skipped = skipped,
            Failed = failed
        };
    }
}
