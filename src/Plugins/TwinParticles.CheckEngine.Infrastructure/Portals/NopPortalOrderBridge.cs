using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Nop.Core;
using Nop.Core.Domain.Common;
using Nop.Core.Domain.Orders;
using Nop.Core.Domain.Payments;
using Nop.Core.Domain.Shipping;
using Nop.Core.Domain.Tax;
using Nop.Services.Catalog;
using Nop.Services.Common;
using Nop.Services.Customers;
using Nop.Services.Directory;
using Nop.Services.Orders;
using TwinParticles.CheckEngine.Domain.Portals;

namespace TwinParticles.CheckEngine.Infrastructure.Portals;

/// <summary>
/// Creates trade orders for vertical portals without routing through the retail cart (FR-1006, AC-46.1).
/// </summary>
public sealed class NopPortalOrderBridge : IPortalOrderBridge
{
    private readonly ICustomerService _customerService;
    private readonly IAddressService _addressService;
    private readonly IOrderService _orderService;
    private readonly IProductService _productService;
    private readonly ICurrencyService _currencyService;
    private readonly IStoreContext _storeContext;
    private readonly IWorkContext _workContext;

    public NopPortalOrderBridge(
        ICustomerService customerService,
        IAddressService addressService,
        IOrderService orderService,
        IProductService productService,
        ICurrencyService currencyService,
        IStoreContext storeContext,
        IWorkContext workContext)
    {
        _customerService = customerService;
        _addressService = addressService;
        _orderService = orderService;
        _productService = productService;
        _currencyService = currencyService;
        _storeContext = storeContext;
        _workContext = workContext;
    }

    public async Task<int> CreateTradeOrderAsync(int customerId, IReadOnlyList<PortalOrderLine> lines, CancellationToken cancellationToken)
    {
        if (lines.Count == 0)
            throw new InvalidOperationException("portal.order.empty");

        var customer = await _customerService.GetCustomerByIdAsync(customerId)
            ?? throw new InvalidOperationException("portal.customer.not_found");

        var store = await _storeContext.GetCurrentStoreAsync();
        var currency = await _workContext.GetWorkingCurrencyAsync();
        var language = await _workContext.GetWorkingLanguageAsync();

        var billingAddress = await ResolveBillingAddressAsync(customer);
        var orderItems = new List<OrderItem>();
        decimal subtotal = 0m;

        foreach (var line in lines)
        {
            var product = await _productService.GetProductByIdAsync(line.ProductId)
                ?? throw new InvalidOperationException($"portal.product.not_found:{line.ProductId}");

            var unitPrice = line.UnitPrice > 0 ? line.UnitPrice : product.Price;
            subtotal += unitPrice * line.Quantity;

            orderItems.Add(new OrderItem
            {
                ProductId = product.Id,
                Quantity = line.Quantity,
                UnitPriceInclTax = unitPrice,
                UnitPriceExclTax = unitPrice,
                PriceInclTax = unitPrice * line.Quantity,
                PriceExclTax = unitPrice * line.Quantity,
                OriginalProductCost = product.ProductCost,
                AttributeDescription = string.Empty,
                AttributesXml = string.Empty,
                DownloadCount = 0,
                IsDownloadActivated = false,
                LicenseDownloadId = 0
            });
        }

        var order = new Order
        {
            StoreId = store.Id,
            OrderGuid = Guid.NewGuid(),
            CustomerId = customer.Id,
            CustomerLanguageId = language.Id,
            CustomerTaxDisplayType = TaxDisplayType.ExcludingTax,
            CustomerCurrencyCode = currency.CurrencyCode,
            CurrencyRate = currency.Rate,
            OrderSubtotalInclTax = subtotal,
            OrderSubtotalExclTax = subtotal,
            OrderTotal = subtotal,
            OrderStatus = OrderStatus.Pending,
            PaymentStatus = PaymentStatus.Paid,
            ShippingStatus = ShippingStatus.NotYetShipped,
            PaymentMethodSystemName = "CheckEngine.TradeAccount",
            BillingAddressId = billingAddress.Id,
            CreatedOnUtc = DateTime.UtcNow,
            CustomOrderNumber = string.Empty
        };

        await _orderService.InsertOrderAsync(order);

        foreach (var item in orderItems)
        {
            item.OrderId = order.Id;
            await _orderService.InsertOrderItemAsync(item);
        }

        order.CustomOrderNumber = $"CE-{order.Id}";
        await _orderService.UpdateOrderAsync(order);
        return order.Id;
    }

    private async Task<Address> ResolveBillingAddressAsync(Nop.Core.Domain.Customers.Customer customer)
    {
        var billingAddress = await _customerService.GetCustomerBillingAddressAsync(customer);
        if (billingAddress is not null)
            return billingAddress;

        billingAddress = new Address
        {
            FirstName = customer.FirstName ?? "Trade",
            LastName = customer.LastName ?? "Account",
            Email = customer.Email ?? "trade@local",
            CountryId = null,
            CreatedOnUtc = DateTime.UtcNow
        };

        await _addressService.InsertAddressAsync(billingAddress);
        customer.BillingAddressId = billingAddress.Id;
        await _customerService.UpdateCustomerAsync(customer);
        return billingAddress;
    }
}
