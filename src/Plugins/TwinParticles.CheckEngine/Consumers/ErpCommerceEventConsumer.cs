using System.Text.Json;
using System.Threading.Tasks;
using Nop.Core.Domain.Customers;
using Nop.Core.Domain.Orders;
using Nop.Services.Events;
using TwinParticles.CheckEngine.Application.Erp;
using TwinParticles.CheckEngine.Domain.Erp;

namespace TwinParticles.CheckEngine.Consumers;

/// <summary>
/// Captures nopCommerce commerce events into the durable ERP outbox. Payloads intentionally contain
/// stable identifiers and event metadata only; raw customer PII is not duplicated into queue storage.
/// </summary>
public sealed class ErpCommerceEventConsumer :
    IConsumer<OrderPlacedEvent>,
    IConsumer<CustomerRegisteredEvent>
{
    private readonly ErpSyncService _syncService;

    public ErpCommerceEventConsumer(ErpSyncService syncService)
    {
        _syncService = syncService;
    }

    public async Task HandleEventAsync(OrderPlacedEvent eventMessage)
    {
        var order = eventMessage.Order;
        var payload = JsonSerializer.Serialize(new
        {
            eventType = "order.placed",
            orderId = order.Id,
            customerId = order.CustomerId,
            createdUtc = order.CreatedOnUtc
        });

        await _syncService.QueueSyncAsync(
            ErpSyncEntityType.Order,
            ErpSyncDirection.PushToErp,
            $"placed:{order.Id}",
            payload,
            default);
    }

    public async Task HandleEventAsync(CustomerRegisteredEvent eventMessage)
    {
        var customer = eventMessage.Customer;
        var payload = JsonSerializer.Serialize(new
        {
            eventType = "customer.registered",
            customerId = customer.Id,
            customerGuid = customer.CustomerGuid
        });

        await _syncService.QueueSyncAsync(
            ErpSyncEntityType.Customer,
            ErpSyncDirection.PushToErp,
            $"registered:{customer.Id}",
            payload,
            default);
    }
}
