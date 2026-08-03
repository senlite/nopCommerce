using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TwinParticles.CheckEngine.Domain.Seo;

namespace TwinParticles.CheckEngine.Infrastructure.Seo;

public sealed class InMemorySeoSitemapService : ISeoSitemapService
{
    private List<string> _urls = [];

    public Task RebuildAsync(IReadOnlyList<SeoLandingPage> pages, CancellationToken cancellationToken)
    {
        _urls = pages
            .Where(x => x.IsIndexable)
            .Select(x => x.UrlPath)
            .Distinct()
            .OrderBy(x => x)
            .ToList();

        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<string>> GetUrlsAsync(CancellationToken cancellationToken)
    {
        return Task.FromResult<IReadOnlyList<string>>(_urls);
    }
}
