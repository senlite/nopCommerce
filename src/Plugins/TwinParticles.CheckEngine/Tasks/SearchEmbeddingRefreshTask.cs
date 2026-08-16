using System.Threading;
using System.Threading.Tasks;
using Nop.Services.ScheduleTasks;
using TwinParticles.CheckEngine.Application.Search;

namespace TwinParticles.CheckEngine.Tasks;

/// <summary>
/// Keeps semantic search embeddings current by incrementally re-indexing stale catalog rows for en/ar.
/// </summary>
public sealed class SearchEmbeddingRefreshTask : IScheduleTask
{
    private static readonly string[] Locales = ["en", "ar"];

    private readonly SearchEmbeddingIndexBuilderService _embeddingIndexBuilderService;

    public SearchEmbeddingRefreshTask(SearchEmbeddingIndexBuilderService embeddingIndexBuilderService)
    {
        _embeddingIndexBuilderService = embeddingIndexBuilderService;
    }

    public async Task ExecuteAsync()
    {
        foreach (var locale in Locales)
        {
            await _embeddingIndexBuilderService.RefreshIncrementalAsync(locale, CancellationToken.None);
        }
    }
}
