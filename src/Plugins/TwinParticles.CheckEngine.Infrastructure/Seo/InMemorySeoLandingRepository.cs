using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TwinParticles.CheckEngine.Domain.Seo;

namespace TwinParticles.CheckEngine.Infrastructure.Seo;

public sealed class InMemorySeoLandingRepository : ISeoLandingRepository
{
    private readonly List<SeoLandingPage> _pages = [];

    public Task UpsertAsync(SeoLandingPage page, CancellationToken cancellationToken)
    {
        var existing = _pages.FirstOrDefault(x => x.Type == page.Type && x.VehicleConfigurationId == page.VehicleConfigurationId && x.ProductId == page.ProductId && x.Locale == page.Locale);
        if (existing is not null)
            _pages.Remove(existing);

        _pages.Add(page);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<SeoLandingPage>> GetAllAsync(CancellationToken cancellationToken)
    {
        return Task.FromResult<IReadOnlyList<SeoLandingPage>>(_pages.ToList());
    }
}
