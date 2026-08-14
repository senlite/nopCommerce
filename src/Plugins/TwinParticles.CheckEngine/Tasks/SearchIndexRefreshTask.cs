using System.Threading.Tasks;
using Nop.Services.ScheduleTasks;
using TwinParticles.CheckEngine.Domain.Search;

namespace TwinParticles.CheckEngine.Tasks;

/// <summary>
/// Keeps the keyword search projection current by incrementally re-projecting products changed since
/// the last cursor. Runs frequently so a published product becomes searchable within the NFR-022
/// freshness budget.
/// </summary>
public sealed class SearchIndexRefreshTask : IScheduleTask
{
    private readonly ISearchIndexHealthService _indexHealthService;

    public SearchIndexRefreshTask(ISearchIndexHealthService indexHealthService)
    {
        _indexHealthService = indexHealthService;
    }

    public Task ExecuteAsync()
    {
        return _indexHealthService.RefreshIncrementalAsync(default);
    }
}
