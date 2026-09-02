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
        => ResolveUnitPriceAsync(priceListId, productId, quantity: 1, accountTierCode: null, cancellationToken);

    public Task<decimal> ResolveUnitPriceAsync(
        int? priceListId,
        int productId,
        int quantity,
        CancellationToken cancellationToken)
        => ResolveUnitPriceAsync(priceListId, productId, quantity, accountTierCode: null, cancellationToken);

    public async Task<decimal> ResolveUnitPriceAsync(
        int? priceListId,
        int productId,
        int quantity,
        string? accountTierCode,
        CancellationToken cancellationToken)
    {
        decimal unitPrice;
        if (priceListId is int listId)
        {
            var tradePrice = await _workshop.ResolveTradePriceAsync(listId, productId, cancellationToken);
            unitPrice = tradePrice > 0m
                ? tradePrice
                : await _catalogPrices.GetProductPriceAsync(productId, cancellationToken);

            if (!string.IsNullOrWhiteSpace(accountTierCode))
            {
                var accountTier = await _workshop.GetAccountTierAsync(accountTierCode.Trim(), cancellationToken);
                if (accountTier is { DiscountPercent: > 0m })
                {
                    unitPrice = ApplyDiscount(unitPrice, accountTier.DiscountPercent);
                }
            }

            if (quantity > 1)
            {
                var tiers = await _workshop.ListPriceTiersAsync(listId, cancellationToken);
                var tier = tiers
                    .Where(t => quantity >= t.MinQuantity)
                    .OrderByDescending(t => t.MinQuantity)
                    .FirstOrDefault();

                if (tier is { DiscountPercent: > 0m })
                    unitPrice = ApplyDiscount(unitPrice, tier.DiscountPercent);
            }

            return unitPrice;
        }

        return await _catalogPrices.GetProductPriceAsync(productId, cancellationToken);
    }

    private static decimal ApplyDiscount(decimal unitPrice, decimal discountPercent)
        => Math.Round(
            unitPrice * (1m - discountPercent / 100m),
            2,
            MidpointRounding.AwayFromZero);
}
