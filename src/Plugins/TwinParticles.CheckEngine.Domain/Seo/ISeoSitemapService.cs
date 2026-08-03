using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace TwinParticles.CheckEngine.Domain.Seo;

public interface ISeoSitemapService
{
    Task RebuildAsync(IReadOnlyList<SeoLandingPage> pages, CancellationToken cancellationToken);

    Task<IReadOnlyList<string>> GetUrlsAsync(CancellationToken cancellationToken);
}
