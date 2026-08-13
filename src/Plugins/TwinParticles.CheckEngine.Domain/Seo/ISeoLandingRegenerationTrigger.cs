using System.Threading;
using System.Threading.Tasks;

namespace TwinParticles.CheckEngine.Domain.Seo;

/// <summary>
/// Regenerates SEO landing pages when a fitment claim's publication state changes.
///
/// Landing indexability is fitment-gated (H1.29), so a claim being published, approved, or
/// rejected must immediately refresh the affected vehicle and part-for-vehicle landings rather
/// than waiting for the next admin rebuild or first request. Implementations must be safe to call
/// from write paths and must not throw when the target product/vehicle has no eligible claims.
/// </summary>
public interface ISeoLandingRegenerationTrigger
{
    Task OnFitmentPublicationChangedAsync(int productId, int vehicleConfigurationId, CancellationToken cancellationToken);
}
