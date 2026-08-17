using System.Linq;
using System.Threading.Tasks;
using Nop.Core.Domain.Orders;
using Nop.Services.Events;
using Nop.Services.Orders;
using TwinParticles.CheckEngine.Application.Marketplace;
using TwinParticles.CheckEngine.Domain.Marketplace;

namespace TwinParticles.CheckEngine.Consumers;

/// <summary>
/// Snapshots commission terms and per-vendor checkout splits when an order is placed (AC-19.2, AC-19.6).
/// </summary>
public sealed class MarketplaceOrderPlacedConsumer : IConsumer<OrderPlacedEvent>
{
    private readonly CommissionSnapshotService _snapshotService;
    private readonly OrderVendorSplitService _splitService;
    private readonly IOrderService _orderService;

    public MarketplaceOrderPlacedConsumer(
        CommissionSnapshotService snapshotService,
        OrderVendorSplitService splitService,
        IOrderService orderService)
    {
        _snapshotService = snapshotService;
        _splitService = splitService;
        _orderService = orderService;
    }

    public async Task HandleEventAsync(OrderPlacedEvent eventMessage)
    {
        var order = eventMessage.Order;
        var items = await _orderService.GetOrderItemsAsync(order.Id);
        var lines = items.Select(item => new VendorOrderLine
        {
            OrderItemId = item.Id,
            ProductId = item.ProductId,
            Quantity = item.Quantity,
            PriceExclTax = item.PriceExclTax
        }).ToList();

        await _splitService.SplitOrderAsync(order.Id, order.CreatedOnUtc, lines, default);
        await _snapshotService.SnapshotOrderAsync(order.Id, order.CreatedOnUtc, lines, default);
    }
}
