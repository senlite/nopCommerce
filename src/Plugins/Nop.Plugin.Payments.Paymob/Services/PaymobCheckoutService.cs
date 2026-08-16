using System.Net.Http.Json;
using System.Text.Json;
using Nop.Core;
using Nop.Core.Domain.Orders;

namespace Nop.Plugin.Payments.Paymob.Services;

public sealed class PaymobCheckoutService
{
    private static readonly Uri LiveBase = new("https://accept.paymob.com/");

    private readonly HttpClient _httpClient;
    private readonly PaymobPaymentSettings _settings;
    private readonly IWebHelper _webHelper;

    public PaymobCheckoutService(HttpClient httpClient, PaymobPaymentSettings settings, IWebHelper webHelper)
    {
        _httpClient = httpClient;
        _settings = settings;
        _webHelper = webHelper;
        _httpClient.BaseAddress ??= LiveBase;
    }

    public bool CanRedirect
        => !_settings.UseSandbox &&
           !string.IsNullOrWhiteSpace(_settings.ApiKey) &&
           !string.IsNullOrWhiteSpace(_settings.IntegrationId) &&
           !string.IsNullOrWhiteSpace(_settings.IframeId);

    public async Task<string?> CreateCheckoutUrlAsync(Order order, CancellationToken cancellationToken)
    {
        if (!CanRedirect || order is null)
            return null;

        var auth = await PostJsonAsync("api/auth/tokens", new { api_key = _settings.ApiKey }, cancellationToken);
        var token = ReadString(auth, "token");
        if (string.IsNullOrWhiteSpace(token))
            return null;

        var amountCents = (int)Math.Round(order.OrderTotal * 100m, MidpointRounding.AwayFromZero);
        var storeLocation = _webHelper.GetStoreLocation();
        var callbackUrl = $"{storeLocation.TrimEnd('/')}/PaymentPaymobPublic/Callback";

        var createdOrder = await PostJsonAsync("api/ecommerce/orders", new
        {
            auth_token = token,
            delivery_needed = "false",
            amount_cents = amountCents.ToString(),
            currency = string.IsNullOrWhiteSpace(order.CustomerCurrencyCode) ? "EGP" : order.CustomerCurrencyCode,
            merchant_order_id = order.OrderGuid.ToString()
        }, cancellationToken);

        var paymobOrderId = ReadInt(createdOrder, "id");
        if (paymobOrderId is null)
            return null;

        var paymentKey = await PostJsonAsync("api/acceptance/payment_keys", new
        {
            auth_token = token,
            amount_cents = amountCents,
            expiration = 3600,
            order_id = paymobOrderId.Value,
            currency = string.IsNullOrWhiteSpace(order.CustomerCurrencyCode) ? "EGP" : order.CustomerCurrencyCode,
            integration_id = int.TryParse(_settings.IntegrationId, out var integrationId) ? integrationId : 0,
            notification_url = callbackUrl,
            redirection_url = callbackUrl
        }, cancellationToken);

        var paymentToken = ReadString(paymentKey, "token");
        if (string.IsNullOrWhiteSpace(paymentToken))
            return null;

        return $"{LiveBase}api/acceptance/iframes/{_settings.IframeId}?payment_token={Uri.EscapeDataString(paymentToken)}";
    }

    private async Task<JsonElement> PostJsonAsync(string path, object body, CancellationToken cancellationToken)
    {
        using var response = await _httpClient.PostAsJsonAsync(path, body, cancellationToken);
        if (!response.IsSuccessStatusCode)
            return default;

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
        return document.RootElement.Clone();
    }

    private static string? ReadString(JsonElement element, string name)
        => element.ValueKind == JsonValueKind.Object && element.TryGetProperty(name, out var value)
            ? value.GetString()
            : null;

    private static int? ReadInt(JsonElement element, string name)
    {
        if (element.ValueKind != JsonValueKind.Object || !element.TryGetProperty(name, out var value))
            return null;

        if (value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out var number))
            return number;

        return int.TryParse(value.GetString(), out var parsed) ? parsed : null;
    }
}
