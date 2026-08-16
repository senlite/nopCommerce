using System.Threading;
using System.Threading.Tasks;
using Nop.Services.ScheduleTasks;
using TwinParticles.CheckEngine.Application.Search;
using TwinParticles.CheckEngine.Domain.Ai;

namespace TwinParticles.CheckEngine.Tasks;

/// <summary>
/// Keeps semantic search embeddings current by incrementally re-indexing stale catalog rows for en/ar.
/// </summary>
public sealed class SearchEmbeddingRefreshTask : IScheduleTask
{
    private static readonly string[] Locales = ["en", "ar"];

    private readonly SearchEmbeddingIndexBuilderService _embeddingIndexBuilderService;
    private readonly IAiFeatureToggle? _featureToggle;

    public SearchEmbeddingRefreshTask(
        SearchEmbeddingIndexBuilderService embeddingIndexBuilderService,
        IAiFeatureToggle? featureToggle = null)
    {
        _embeddingIndexBuilderService = embeddingIndexBuilderService;
        _featureToggle = featureToggle;
    }

    public async Task ExecuteAsync()
    {
        if (_featureToggle is not null && !_featureToggle.IsEnabled(AiFeatureKeys.SearchSemantic))
            return;

        foreach (var locale in Locales)
        {
            await _embeddingIndexBuilderService.RefreshIncrementalAsync(locale, CancellationToken.None);
        }
    }
}
