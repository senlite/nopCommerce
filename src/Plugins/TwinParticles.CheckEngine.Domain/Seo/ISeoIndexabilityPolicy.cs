using System.Threading;
using System.Threading.Tasks;

namespace TwinParticles.CheckEngine.Domain.Seo;

/// <summary>
/// Decides whether a landing has enough verified, sellable content to be indexed. Empty or thin
/// pages remain publicly reachable for users but emit noindex and stay out of the sitemap (FR-440).
/// </summary>
public interface ISeoIndexabilityPolicy
{
    Task<bool> IsVehicleLandingIndexableAsync(int vehicleConfigurationId, CancellationToken cancellationToken);

    Task<bool> IsPartForVehicleLandingIndexableAsync(
        int productId,
        int vehicleConfigurationId,
        CancellationToken cancellationToken);
}
