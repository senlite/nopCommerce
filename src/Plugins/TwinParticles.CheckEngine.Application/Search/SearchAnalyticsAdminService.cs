using System;
using System.Threading;
using System.Threading.Tasks;
using TwinParticles.CheckEngine.Domain.Search;

namespace TwinParticles.CheckEngine.Application.Search;

public sealed class SearchAnalyticsAdminService
{
    private readonly ISearchAnalyticsService _analyticsService;

    public SearchAnalyticsAdminService(ISearchAnalyticsService analyticsService)
    {
        _analyticsService = analyticsService;
    }

    public Task<SearchAnalyticsSummary> GetSummaryAsync(int days, CancellationToken cancellationToken)
    {
        var safeDays = Math.Clamp(days, 1, 365);
        return _analyticsService.GetSummaryAsync(DateTime.UtcNow.AddDays(-safeDays), cancellationToken);
    }

    public Task<int> PruneAsync(int retentionDays, CancellationToken cancellationToken)
    {
        var safeDays = Math.Clamp(retentionDays, 30, 730);
        return _analyticsService.PruneAsync(DateTime.UtcNow.AddDays(-safeDays), cancellationToken);
    }
}
