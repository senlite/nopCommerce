using System.Threading;
using System.Threading.Tasks;
using TwinParticles.CheckEngine.Domain.Portals;
using TwinParticles.CheckEngine.Domain.Workshop;

namespace TwinParticles.CheckEngine.Application.Portals;

/// <summary>
/// Resolves trade unit prices from optional price lists with catalog fallback.
/// </summary>
public sealed class PortalTradePricingService
{
    private readonly IWorkshopJobRepository _workshop;
    private readonly IPortalCatalogPriceProvider _catalogPrices;

    public PortalTradePricingService(IWorkshopJobRepository workshop, IPortalCatalogPriceProvider catalogPrices)
    {
        _workshop = workshop;
        _catalogPrices = catalogPrices;
    }

    public async Task<decimal> ResolveUnitPriceAsync(int? priceListId, int productId, CancellationToken cancellationToken)
    {
        if (priceListId is int listId)
        {
            var tradePrice = await _workshop.ResolveTradePriceAsync(listId, productId, cancellationToken);
            if (tradePrice > 0m)
                return tradePrice;
        }

        return await _catalogPrices.GetProductPriceAsync(productId, cancellationToken);
    }
}
