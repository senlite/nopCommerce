using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nop.Core.Domain.Customers;
using Nop.Core.Domain.Orders;
using Nop.Services.Events;
using TwinParticles.CheckEngine.Application.Erp;
using TwinParticles.CheckEngine.Configuration;
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

        try
        {
            await _syncService.QueueSyncAsync(
                ErpSyncEntityType.Order,
                ErpSyncDirection.PushToErp,
                $"placed:{order.Id}",
                payload,
                default);
        }
        catch (System.Exception exception)
        {
            // ERP downtime or queue persistence failure must never roll back checkout (FR-830).
            _logger.LogWarning(exception, "Unable to enqueue ERP order event for order {OrderId}", order.Id);
        }
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

        try
        {
            await _syncService.QueueSyncAsync(
                ErpSyncEntityType.Customer,
                ErpSyncDirection.PushToErp,
                $"registered:{customer.Id}",
                payload,
                default);
        }
        catch (System.Exception exception)
        {
            // Registration remains available even when the optional ERP integration is degraded.
            _logger.LogWarning(exception, "Unable to enqueue ERP customer event for customer {CustomerId}", customer.Id);
        }
    }
}
