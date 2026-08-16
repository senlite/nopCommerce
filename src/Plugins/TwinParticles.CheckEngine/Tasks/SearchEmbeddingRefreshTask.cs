using System.Threading;
using System.Threading.Tasks;
using Nop.Services.ScheduleTasks;
using TwinParticles.CheckEngine.Application.Search;
using TwinParticles.CheckEngine.Domain.Ai;
using TwinParticles.CheckEngine.Domain.Search;

namespace TwinParticles.CheckEngine.Tasks;

/// <summary>
/// Keeps semantic search embeddings current by incrementally re-indexing stale catalog rows for en/ar.
/// Bootstraps a full rebuild when a locale has catalog rows but no vectors yet.
/// </summary>
public sealed class SearchEmbeddingRefreshTask : IScheduleTask
{
    private static readonly string[] Locales = ["en", "ar"];

    private readonly SearchEmbeddingIndexBuilderService _embeddingIndexBuilderService;
    private readonly ISearchEmbeddingCatalogSource _catalogSource;
    private readonly ISearchEmbeddingIndex _embeddingIndex;
    private readonly IAiFeatureToggle? _featureToggle;

    public SearchEmbeddingRefreshTask(
        SearchEmbeddingIndexBuilderService embeddingIndexBuilderService,
        ISearchEmbeddingCatalogSource catalogSource,
        ISearchEmbeddingIndex embeddingIndex,
        IAiFeatureToggle? featureToggle = null)
    {
        _embeddingIndexBuilderService = embeddingIndexBuilderService;
        _catalogSource = catalogSource;
        _embeddingIndex = embeddingIndex;
        _featureToggle = featureToggle;
    }

    public async Task ExecuteAsync()
    {
        if (_featureToggle is not null && !_featureToggle.IsEnabled(AiFeatureKeys.SearchSemantic))
            return;

        foreach (var locale in Locales)
        {
            var catalogCount = await _catalogSource.GetCatalogCountAsync(locale, CancellationToken.None);
            if (catalogCount == 0)
                continue;

            var vectorCount = await _embeddingIndex.GetCountAsync(locale, CancellationToken.None);
            if (vectorCount == 0)
                await _embeddingIndexBuilderService.RebuildAsync(locale, CancellationToken.None);
            else
                await _embeddingIndexBuilderService.RefreshIncrementalAsync(locale, CancellationToken.None);
        }
    }
}
