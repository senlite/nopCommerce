using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Nop.Plugin.Payments.Paymob.Services;
using Nop.Services.Orders;
using Nop.Web.Framework.Controllers;
using Nop.Web.Framework.Mvc.Filters;

namespace Nop.Plugin.Payments.Paymob.Controllers;

public class PaymentPaymobPublicController : BasePaymentController
{
    private readonly IOrderProcessingService _orderProcessingService;
    private readonly IOrderService _orderService;
    private readonly PaymobHmacValidator _hmacValidator;
    private readonly PaymobPaymentSettings _settings;

    public PaymentPaymobPublicController(
        IOrderProcessingService orderProcessingService,
        IOrderService orderService,
        PaymobPaymentSettings settings)
    {
        _orderProcessingService = orderProcessingService;
        _orderService = orderService;
        _settings = settings;
        _hmacValidator = new PaymobHmacValidator();
    }

    [HttpGet]
    [HttpPost]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> Callback()
    {
        if (string.IsNullOrWhiteSpace(_settings.HmacSecret))
            return BadRequest();

        var fields = Request.HasFormContentType
            ? PaymobCallbackFieldReader.FromForm(Request.Form)
            : PaymobCallbackFieldReader.FromQuery(Request.Query);

        var hmac = PaymobCallbackFieldReader.ReadHmac(fields, Request.Query["hmac"]);
        if (!_hmacValidator.Verify(fields, _settings.HmacSecret, hmac))
            return BadRequest();

        if (!PaymobCallbackFieldReader.IsSuccessful(fields))
            return Ok();

        var order = await ResolveOrderAsync(fields);
        if (order is not null && !_orderProcessingService.CanMarkOrderAsPaid(order))
            return Ok();

        if (order is not null)
            await _orderProcessingService.MarkOrderAsPaidAsync(order);

        return Ok();
    }

    private async Task<Nop.Core.Domain.Orders.Order?> ResolveOrderAsync(IReadOnlyDictionary<string, string?> fields)
    {
        var guid = PaymobCallbackFieldReader.TryReadOrderGuid(fields);
        if (guid.HasValue)
            return await _orderService.GetOrderByGuidAsync(guid.Value);

        var id = PaymobCallbackFieldReader.TryReadOrderId(fields);
        if (id.HasValue)
            return await _orderService.GetOrderByIdAsync(id.Value);

        return null;
    }
}
