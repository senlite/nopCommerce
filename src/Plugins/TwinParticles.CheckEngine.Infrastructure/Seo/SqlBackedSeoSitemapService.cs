using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TwinParticles.CheckEngine.Domain.Seo;

namespace TwinParticles.CheckEngine.Infrastructure.Seo;

/// <summary>
/// Projects sitemap URLs directly from the durable landing repository. Rebuild is intentionally a
/// no-op: upserting a landing is the incremental rebuild, and every read reflects SQL immediately.
/// This survives process restarts and works consistently across nodes.
/// </summary>
public sealed class SqlBackedSeoSitemapService : ISeoSitemapService
{
    private readonly ISeoLandingRepository _repository;

    public SqlBackedSeoSitemapService(ISeoLandingRepository repository)
    {
        _repository = repository;
    }

    public Task RebuildAsync(IReadOnlyList<SeoLandingPage> pages, CancellationToken cancellationToken)
        => Task.CompletedTask;

    public async Task<IReadOnlyList<string>> GetUrlsAsync(CancellationToken cancellationToken)
    {
        var pages = await _repository.GetAllAsync(cancellationToken);
        return pages
            .Where(page => page.IsIndexable)
            .Select(page => page.UrlPath)
            .Where(path => !string.IsNullOrWhiteSpace(path))
            .Distinct()
            .OrderBy(path => path)
            .ToList();
    }
}
