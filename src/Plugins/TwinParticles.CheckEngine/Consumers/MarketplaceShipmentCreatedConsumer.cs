using System.Linq;
using System.Threading.Tasks;
using Nop.Core.Domain.Shipping;
using Nop.Services.Events;
using Nop.Services.Orders;
using Nop.Services.Shipping;
using TwinParticles.CheckEngine.Application.Marketplace;
using TwinParticles.CheckEngine.Domain.Marketplace;

namespace TwinParticles.CheckEngine.Consumers;

/// <summary>
/// FR-855: associates each shipment with the vendor(s) whose lines it contains.
/// </summary>
public sealed class MarketplaceShipmentCreatedConsumer : IConsumer<ShipmentCreatedEvent>
{
    private readonly OrderVendorSplitService _splitService;
    private readonly IShipmentService _shipmentService;
    private readonly IOrderService _orderService;

    public MarketplaceShipmentCreatedConsumer(
        OrderVendorSplitService splitService,
        IShipmentService shipmentService,
        IOrderService orderService)
    {
        _splitService = splitService;
        _shipmentService = shipmentService;
        _orderService = orderService;
    }

    public async Task HandleEventAsync(ShipmentCreatedEvent eventMessage)
    {
        var shipment = eventMessage.Shipment;
        var shipmentItems = await _shipmentService.GetShipmentItemsByShipmentIdAsync(shipment.Id);
        var orderItems = await _orderService.GetOrderItemsAsync(shipment.OrderId);
        var lines = orderItems.Select(item => new VendorOrderLine
        {
            OrderItemId = item.Id,
            ProductId = item.ProductId,
            Quantity = item.Quantity,
            PriceExclTax = item.PriceExclTax
        }).ToList();

        var refs = shipmentItems
            .Select(item => new ShipmentItemRef { OrderItemId = item.OrderItemId, Quantity = item.Quantity })
            .ToList();

        await _splitService.MapShipmentAsync(
            shipment.Id,
            shipment.OrderId,
            refs,
            lines,
            default);
    }
}
