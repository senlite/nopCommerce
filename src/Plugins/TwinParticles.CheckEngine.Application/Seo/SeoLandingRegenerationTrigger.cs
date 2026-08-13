using System.Threading;
using System.Threading.Tasks;
using TwinParticles.CheckEngine.Domain.Seo;

namespace TwinParticles.CheckEngine.Application.Seo;

/// <summary>
/// Event-driven regenerator for fitment-gated landings. When a claim's publication state changes,
/// it rebuilds both the vehicle landing and the part-for-vehicle landing in each supported locale
/// so their <see cref="SeoLandingPage.IsIndexable"/> flag reflects the current fitment truth, then
/// refreshes the sitemap projection.
/// </summary>
public sealed class SeoLandingRegenerationTrigger : ISeoLandingRegenerationTrigger
{
    private static readonly string[] Locales = ["en", "ar"];

    private readonly SeoLandingService _landingService;

    public SeoLandingRegenerationTrigger(SeoLandingService landingService)
    {
        _landingService = landingService;
    }

    public async Task OnFitmentPublicationChangedAsync(int productId, int vehicleConfigurationId, CancellationToken cancellationToken)
    {
        if (vehicleConfigurationId <= 0)
            return;

        foreach (var locale in Locales)
        {
            await _landingService.GenerateVehicleLandingAsync(vehicleConfigurationId, locale, cancellationToken);

            if (productId > 0)
                await _landingService.GeneratePartForVehicleLandingAsync(productId, vehicleConfigurationId, locale, cancellationToken);
        }

        await _landingService.RebuildSitemapAsync(cancellationToken);
    }
}
