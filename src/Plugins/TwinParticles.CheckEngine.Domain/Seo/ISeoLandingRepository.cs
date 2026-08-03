using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace TwinParticles.CheckEngine.Domain.Seo;

public interface ISeoLandingRepository
{
    Task UpsertAsync(SeoLandingPage page, CancellationToken cancellationToken);

    Task<IReadOnlyList<SeoLandingPage>> GetAllAsync(CancellationToken cancellationToken);
}
