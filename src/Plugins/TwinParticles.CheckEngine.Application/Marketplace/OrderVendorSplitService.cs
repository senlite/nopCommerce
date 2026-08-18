using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TwinParticles.CheckEngine.Application.Licensing;
using TwinParticles.CheckEngine.Domain.Marketplace;

namespace TwinParticles.CheckEngine.Application.Marketplace;

/// <summary>
/// FR-855: groups order lines by vendor at checkout and maps shipments to vendors.
/// </summary>
public sealed class OrderVendorSplitService
{
    private readonly IOrderVendorSplitStore _store;
    private readonly IVendorOwnershipStore _ownership;
    private readonly IVendorRepository _vendors;
    private readonly MarketplaceLicenceGate _licenceGate;

    public OrderVendorSplitService(
        IOrderVendorSplitStore store,
        IVendorOwnershipStore ownership,
        IVendorRepository vendors,
        MarketplaceLicenceGate licenceGate)
    {
        _store = store;
        _ownership = ownership;
        _vendors = vendors;
        _licenceGate = licenceGate;
    }

    public async Task<OrderCheckoutGroup?> SplitOrderAsync(
        int orderId,
        DateTime orderCreatedUtc,
        IReadOnlyList<VendorOrderLine> items,
        CancellationToken cancellationToken)
    {
        if (!await _licenceGate.AllowsMarketplaceAsync(cancellationToken) || items.Count == 0)
            return null;

        if (await _store.ExistsForOrderAsync(orderId, cancellationToken))
            return await _store.GetByOrderIdAsync(orderId, cancellationToken);

        var vendorLines = new Dictionary<int, List<VendorOrderLine>>();
        foreach (var item in items)
        {
            var vendorId = await ResolveVendorIdAsync(item.ProductId, cancellationToken);
            if (!vendorId.HasValue)
                continue;

            if (!vendorLines.TryGetValue(vendorId.Value, out var lines))
            {
                lines = [];
                vendorLines[vendorId.Value] = lines;
            }

            lines.Add(item);
        }

        if (vendorLines.Count == 0)
            return null;

        var checkoutGroupId = Guid.NewGuid();
        var splits = vendorLines
            .OrderBy(pair => pair.Key)
            .Select(pair => new OrderVendorSplit
            {
                CheckoutGroupId = checkoutGroupId,
                ParentOrderId = orderId,
                VendorId = pair.Key,
                LineSubtotalExclTax = pair.Value.Sum(line => line.PriceExclTax),
                Lines = pair.Value
                    .OrderBy(line => line.OrderItemId)
                    .Select(line => new OrderVendorSplitLine
                    {
                        OrderItemId = line.OrderItemId,
                        ProductId = line.ProductId,
                        Quantity = line.Quantity,
                        LineSubtotalExclTax = line.PriceExclTax
                    })
                    .ToList()
            })
            .ToList();

        var group = new OrderCheckoutGroup
        {
            CheckoutGroupId = checkoutGroupId,
            ParentOrderId = orderId,
            CreatedUtc = orderCreatedUtc,
            Splits = splits
        };

        await _store.SaveCheckoutGroupAsync(group, cancellationToken);
        return group;
    }

    public Task<OrderCheckoutGroup?> GetCheckoutGroupAsync(int orderId, CancellationToken cancellationToken)
        => _store.GetByOrderIdAsync(orderId, cancellationToken);

    public async Task MapShipmentAsync(
        int shipmentId,
        int orderId,
        IReadOnlyList<ShipmentItemRef> shipmentItems,
        IReadOnlyList<VendorOrderLine> orderItems,
        CancellationToken cancellationToken)
    {
        if (!await _licenceGate.AllowsMarketplaceAsync(cancellationToken))
            return;

        if (await _store.ShipmentMapExistsAsync(shipmentId, cancellationToken))
            return;

        if (shipmentItems.Count == 0)
            return;

        var orderItemLookup = orderItems.ToDictionary(item => item.OrderItemId);
        var vendorIds = new HashSet<int>();

        foreach (var shipmentItem in shipmentItems)
        {
            if (!orderItemLookup.TryGetValue(shipmentItem.OrderItemId, out var orderLine))
                continue;

            var vendorId = await ResolveVendorIdAsync(orderLine.ProductId, cancellationToken);
            if (vendorId.HasValue)
                vendorIds.Add(vendorId.Value);
        }

        if (vendorIds.Count == 0)
            return;

        var createdUtc = DateTime.UtcNow;
        var maps = vendorIds
            .OrderBy(id => id)
            .Select(vendorId => new ShipmentVendorMap
            {
                ShipmentId = shipmentId,
                OrderId = orderId,
                VendorId = vendorId,
                CreatedUtc = createdUtc
            })
            .ToList();

        await _store.SaveShipmentVendorMapsAsync(maps, cancellationToken);
    }

    public Task<IReadOnlyList<ShipmentVendorMap>> GetShipmentMapsAsync(int orderId, CancellationToken cancellationToken)
        => _store.GetShipmentMapsByOrderIdAsync(orderId, cancellationToken);

    private async Task<int?> ResolveVendorIdAsync(int productId, CancellationToken cancellationToken)
    {
        var vendorId = await _ownership.GetProductVendorIdAsync(productId, cancellationToken);
        if (vendorId.HasValue)
            return vendorId;

        var operatorVendor = await _vendors.GetOperatorAsync(cancellationToken);
        return operatorVendor?.Id;
    }
}

public sealed class ShipmentItemRef
{
    public int OrderItemId { get; init; }

    public int Quantity { get; init; }
}
