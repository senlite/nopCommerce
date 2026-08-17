using System.Linq;
using System.Threading.Tasks;
using Nop.Core.Domain.Orders;
using Nop.Services.Events;
using Nop.Services.Orders;
using TwinParticles.CheckEngine.Application.Marketplace;
using TwinParticles.CheckEngine.Domain.Marketplace;

namespace TwinParticles.CheckEngine.Consumers;

/// <summary>
/// Snapshots commission terms when an order is placed so later rule changes cannot alter settlement (AC-19.6).
/// </summary>
public sealed class MarketplaceOrderPlacedConsumer : IConsumer<OrderPlacedEvent>
{
    private readonly CommissionSnapshotService _snapshotService;
    private readonly IOrderService _orderService;

    public MarketplaceOrderPlacedConsumer(
        CommissionSnapshotService snapshotService,
        IOrderService orderService)
    {
        _snapshotService = snapshotService;
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

        await _snapshotService.SnapshotOrderAsync(order.Id, order.CreatedOnUtc, lines, default);
    }
}
