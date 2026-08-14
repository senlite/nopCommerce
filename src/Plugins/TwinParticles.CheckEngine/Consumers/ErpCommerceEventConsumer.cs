using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nop.Core.Domain.Catalog;
using Nop.Core.Domain.Customers;
using Nop.Core.Domain.Orders;
using Nop.Core.Domain.Shipping;
using Nop.Core.Events;
using Nop.Services.Events;
using TwinParticles.CheckEngine.Application.Erp;
using TwinParticles.CheckEngine.Domain.Configuration;
using TwinParticles.CheckEngine.Domain.Erp;

namespace TwinParticles.CheckEngine.Consumers;

/// <summary>
/// Captures nopCommerce commerce events into the durable ERP outbox. Payloads intentionally contain
/// stable identifiers and event metadata only; raw customer PII is not duplicated into queue storage.
/// </summary>
public sealed class ErpCommerceEventConsumer :
    IConsumer<OrderPlacedEvent>,
    IConsumer<OrderPaidEvent>,
    IConsumer<OrderRefundedEvent>,
    IConsumer<OrderStatusChangedEvent>,
    IConsumer<CustomerRegisteredEvent>,
    IConsumer<EntityUpdatedEvent<Customer>>,
    IConsumer<EntityUpdatedEvent<Product>>,
    IConsumer<ShipmentCreatedEvent>,
    IConsumer<ShipmentSentEvent>
{
    private readonly ErpSyncService _syncService;
    private readonly IOptions<CheckEngineSettings> _settings;
    private readonly ILogger<ErpCommerceEventConsumer> _logger;

    public ErpCommerceEventConsumer(
        ErpSyncService syncService,
        IOptions<CheckEngineSettings> settings,
        ILogger<ErpCommerceEventConsumer> logger)
    {
        _syncService = syncService;
        _settings = settings;
        _logger = logger;
    }

    public async Task HandleEventAsync(OrderPlacedEvent eventMessage)
    {
        if (!_settings.Value.Erp.Enabled)
            return;

        var order = eventMessage.Order;
        var payload = JsonSerializer.Serialize(new
        {
            eventType = "order.placed",
            orderId = order.Id,
            customerId = order.CustomerId,
            createdUtc = order.CreatedOnUtc
        });

        await TryQueueAsync(ErpSyncEntityType.Order, $"placed:{order.Id}", payload, order.Id);
    }

    public async Task HandleEventAsync(CustomerRegisteredEvent eventMessage)
    {
        if (!_settings.Value.Erp.Enabled)
            return;

        var customer = eventMessage.Customer;
        var payload = JsonSerializer.Serialize(new
        {
            eventType = "customer.registered",
            customerId = customer.Id,
            customerGuid = customer.CustomerGuid
        });

        await TryQueueAsync(ErpSyncEntityType.Customer, $"registered:{customer.Id}", payload, customer.Id);
    }

    public Task HandleEventAsync(OrderPaidEvent eventMessage)
        => TryQueueAsync(
            ErpSyncEntityType.Invoice,
            $"paid:{eventMessage.Order.Id}",
            JsonSerializer.Serialize(new
            {
                eventType = "order.paid",
                orderId = eventMessage.Order.Id,
                customerId = eventMessage.Order.CustomerId,
                paymentTotal = eventMessage.Order.OrderTotal,
                paidUtc = eventMessage.Order.PaidDateUtc
            }),
            eventMessage.Order.Id);

    public Task HandleEventAsync(OrderRefundedEvent eventMessage)
        => TryQueueAsync(
            ErpSyncEntityType.Return,
            $"refund:{eventMessage.Order.Id}:{eventMessage.Order.RefundedAmount}",
            JsonSerializer.Serialize(new
            {
                eventType = "order.refunded",
                orderId = eventMessage.Order.Id,
                refundedAmount = eventMessage.Amount,
                cumulativeRefundedAmount = eventMessage.Order.RefundedAmount
            }),
            eventMessage.Order.Id);

    public Task HandleEventAsync(OrderStatusChangedEvent eventMessage)
        => TryQueueAsync(
            ErpSyncEntityType.Order,
            $"status:{eventMessage.Order.Id}:{eventMessage.Order.OrderStatusId}",
            JsonSerializer.Serialize(new
            {
                eventType = "order.status_changed",
                orderId = eventMessage.Order.Id,
                previousStatus = (int)eventMessage.PreviousOrderStatus,
                currentStatus = eventMessage.Order.OrderStatusId
            }),
            eventMessage.Order.Id);

    public Task HandleEventAsync(EntityUpdatedEvent<Customer> eventMessage)
    {
        var customer = eventMessage.Entity;
        return TryQueueAsync(
            ErpSyncEntityType.Customer,
            $"updated:{customer.Id}:{customer.LastActivityDateUtc.Ticks}",
            JsonSerializer.Serialize(new
            {
                eventType = "customer.updated",
                customerId = customer.Id,
                customerGuid = customer.CustomerGuid,
                versionUtc = customer.LastActivityDateUtc
            }),
            customer.Id);
    }

    public async Task HandleEventAsync(EntityUpdatedEvent<Product> eventMessage)
    {
        var product = eventMessage.Entity;
        var version = product.UpdatedOnUtc.Ticks;
        var shared = new
        {
            productId = product.Id,
            sku = product.Sku,
            versionUtc = product.UpdatedOnUtc
        };
        await TryQueueAsync(
            ErpSyncEntityType.Product,
            $"updated:{product.Id}:{version}",
            JsonSerializer.Serialize(new { eventType = "product.updated", shared.productId, shared.sku, shared.versionUtc }),
            product.Id);
        await TryQueueAsync(
            ErpSyncEntityType.Inventory,
            $"updated:{product.Id}:{version}",
            JsonSerializer.Serialize(new
            {
                eventType = "inventory.updated",
                shared.productId,
                shared.sku,
                stockQuantity = product.StockQuantity,
                shared.versionUtc
            }),
            product.Id);
    }

    public Task HandleEventAsync(ShipmentCreatedEvent eventMessage)
        => QueueShipmentAsync(eventMessage.Shipment, "shipment.created");

    public Task HandleEventAsync(ShipmentSentEvent eventMessage)
        => QueueShipmentAsync(eventMessage.Shipment, "shipment.sent");

    private Task QueueShipmentAsync(Shipment shipment, string eventType)
        => TryQueueAsync(
            ErpSyncEntityType.Shipment,
            $"{eventType}:{shipment.Id}",
            JsonSerializer.Serialize(new
            {
                eventType,
                shipmentId = shipment.Id,
                orderId = shipment.OrderId,
                trackingNumber = shipment.TrackingNumber
            }),
            shipment.Id);

    private async Task TryQueueAsync(
        ErpSyncEntityType entityType,
        string localId,
        string payload,
        int entityId)
    {
        if (!_settings.Value.Erp.Enabled)
            return;

        try
        {
            await _syncService.QueueSyncAsync(
                entityType,
                ErpSyncDirection.PushToErp,
                localId,
                payload,
                default);
        }
        catch (System.Exception exception)
        {
            // Optional ERP integration must never roll back a host catalog/checkout/customer event.
            _logger.LogWarning(
                exception,
                "Unable to enqueue ERP {EntityType} event for entity {EntityId}",
                entityType,
                entityId);
        }
    }
}
