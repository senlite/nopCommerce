using System.Threading;
using System.Threading.Tasks;

namespace TwinParticles.CheckEngine.Domain.Portals;

/// <summary>
/// Resolves catalog list prices for portal trade-pricing fallback (no Nop dependency in Application).
/// </summary>
public interface IPortalCatalogPriceProvider
{
    Task<decimal> GetProductPriceAsync(int productId, CancellationToken cancellationToken);
}
