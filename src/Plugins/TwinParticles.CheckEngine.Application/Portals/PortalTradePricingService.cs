using System;
using System.Linq;
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

    public Task<decimal> ResolveUnitPriceAsync(int? priceListId, int productId, CancellationToken cancellationToken)
        => ResolveUnitPriceAsync(priceListId, productId, quantity: 1, cancellationToken);

    public async Task<decimal> ResolveUnitPriceAsync(
        int? priceListId,
        int productId,
        int quantity,
        CancellationToken cancellationToken)
    {
        decimal unitPrice;
        if (priceListId is int listId)
        {
            var tradePrice = await _workshop.ResolveTradePriceAsync(listId, productId, cancellationToken);
            unitPrice = tradePrice > 0m
                ? tradePrice
                : await _catalogPrices.GetProductPriceAsync(productId, cancellationToken);

            if (quantity > 1)
            {
                var tiers = await _workshop.ListPriceTiersAsync(listId, cancellationToken);
                var tier = tiers
                    .Where(t => quantity >= t.MinQuantity)
                    .OrderByDescending(t => t.MinQuantity)
                    .FirstOrDefault();

                if (tier is not null && tier.DiscountPercent > 0m)
                {
                    unitPrice = Math.Round(
                        unitPrice * (1m - tier.DiscountPercent / 100m),
                        2,
                        MidpointRounding.AwayFromZero);
                }
            }

            return unitPrice;
        }

        return await _catalogPrices.GetProductPriceAsync(productId, cancellationToken);
    }
}
