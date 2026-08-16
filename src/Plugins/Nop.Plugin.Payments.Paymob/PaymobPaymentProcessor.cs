using Microsoft.AspNetCore.Http;
using Nop.Core;
using Nop.Core.Domain.Orders;
using Nop.Plugin.Payments.Paymob.Components;
using Nop.Plugin.Payments.Paymob.Services;
using Nop.Services.Configuration;
using Nop.Services.Localization;
using Nop.Services.Orders;
using Nop.Services.Payments;
using Nop.Services.Plugins;

namespace Nop.Plugin.Payments.Paymob;

/// <summary>
/// Reference Paymob (Accept) redirect payment method for the Egyptian market. Uses only nopCommerce
/// payment abstractions and has zero dependency on the Check Engine core plugin (EP-17, FR-950/951).
/// </summary>
public class PaymobPaymentProcessor : BasePlugin, IPaymentMethod
{
    private readonly ILocalizationService _localizationService;
    private readonly IOrderTotalCalculationService _orderTotalCalculationService;
    private readonly ISettingService _settingService;
    private readonly IWebHelper _webHelper;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly PaymobCheckoutService _checkoutService;
    private readonly PaymobPaymentSettings _settings;

    public PaymobPaymentProcessor(
        ILocalizationService localizationService,
        IOrderTotalCalculationService orderTotalCalculationService,
        ISettingService settingService,
        IWebHelper webHelper,
        IHttpContextAccessor httpContextAccessor,
        PaymobCheckoutService checkoutService,
        PaymobPaymentSettings settings)
    {
        _localizationService = localizationService;
        _orderTotalCalculationService = orderTotalCalculationService;
        _settingService = settingService;
        _webHelper = webHelper;
        _httpContextAccessor = httpContextAccessor;
        _checkoutService = checkoutService;
        _settings = settings;
    }

    public Task<ProcessPaymentResult> ProcessPaymentAsync(ProcessPaymentRequest processPaymentRequest)
        => Task.FromResult(new ProcessPaymentResult());

    public async Task PostProcessPaymentAsync(PostProcessPaymentRequest postProcessPaymentRequest)
    {
        ArgumentNullException.ThrowIfNull(postProcessPaymentRequest);

        var checkoutUrl = await _checkoutService.CreateCheckoutUrlAsync(
            postProcessPaymentRequest.Order,
            CancellationToken.None);
        if (string.IsNullOrWhiteSpace(checkoutUrl))
            return;

        _httpContextAccessor.HttpContext?.Response.Redirect(checkoutUrl);
    }

    public Task<bool> HidePaymentMethodAsync(IList<ShoppingCartItem> cart)
        => Task.FromResult(string.IsNullOrWhiteSpace(_settings.IntegrationId));

    public async Task<decimal> GetAdditionalHandlingFeeAsync(IList<ShoppingCartItem> cart)
        => await _orderTotalCalculationService.CalculatePaymentAdditionalFeeAsync(
            cart, _settings.AdditionalFee, _settings.AdditionalFeePercentage);

    public Task<CapturePaymentResult> CaptureAsync(CapturePaymentRequest capturePaymentRequest)
        => Task.FromResult(new CapturePaymentResult { Errors = ["Capture not supported"] });

    public Task<RefundPaymentResult> RefundAsync(RefundPaymentRequest refundPaymentRequest)
        => Task.FromResult(new RefundPaymentResult { Errors = ["Refund not supported"] });

    public Task<VoidPaymentResult> VoidAsync(VoidPaymentRequest voidPaymentRequest)
        => Task.FromResult(new VoidPaymentResult { Errors = ["Void not supported"] });

    public Task<ProcessPaymentResult> ProcessRecurringPaymentAsync(ProcessPaymentRequest processPaymentRequest)
        => Task.FromResult(new ProcessPaymentResult { Errors = ["Recurring payment not supported"] });

    public Task<CancelRecurringPaymentResult> CancelRecurringPaymentAsync(CancelRecurringPaymentRequest cancelPaymentRequest)
        => Task.FromResult(new CancelRecurringPaymentResult { Errors = ["Recurring payment not supported"] });

    public Task<bool> CanRePostProcessPaymentAsync(Order order)
    {
        ArgumentNullException.ThrowIfNull(order);
        // Redirection method: the customer can retry the hosted payment while the order is pending.
        return Task.FromResult(true);
    }

    public Task<IList<string>> ValidatePaymentFormAsync(IFormCollection form)
        => Task.FromResult<IList<string>>([]);

    public Task<ProcessPaymentRequest> GetPaymentInfoAsync(IFormCollection form)
        => Task.FromResult(new ProcessPaymentRequest());

    public override string GetConfigurationPageUrl()
        => $"{_webHelper.GetStoreLocation()}Admin/PaymentPaymob/Configure";

    public Type GetPublicViewComponent()
        => typeof(PaymobViewComponent);

    public override async Task InstallAsync()
    {
        await _settingService.SaveSettingAsync(new PaymobPaymentSettings { UseSandbox = true });

        await _localizationService.AddOrUpdateLocaleResourceAsync(new Dictionary<string, string>
        {
            ["Plugins.Payments.Paymob.Fields.ApiKey"] = "API key",
            ["Plugins.Payments.Paymob.Fields.ApiKey.Hint"] = "Your Paymob API key (kept as a secret; never committed).",
            ["Plugins.Payments.Paymob.Fields.IntegrationId"] = "Integration id",
            ["Plugins.Payments.Paymob.Fields.IntegrationId.Hint"] = "The Paymob card integration id.",
            ["Plugins.Payments.Paymob.Fields.IframeId"] = "Iframe id",
            ["Plugins.Payments.Paymob.Fields.IframeId.Hint"] = "The Paymob hosted checkout iframe id.",
            ["Plugins.Payments.Paymob.Fields.HmacSecret"] = "HMAC secret",
            ["Plugins.Payments.Paymob.Fields.HmacSecret.Hint"] = "Used to verify transaction callbacks. Callbacks failing verification are rejected.",
            ["Plugins.Payments.Paymob.Fields.UseSandbox"] = "Use sandbox",
            ["Plugins.Payments.Paymob.Fields.UseSandbox.Hint"] = "When enabled the plugin stays inert and never contacts Paymob.",
            ["Plugins.Payments.Paymob.PaymentMethodDescription"] = "You will be redirected to Paymob to complete your payment."
        });

        await base.InstallAsync();
    }

    public override async Task UninstallAsync()
    {
        await _settingService.DeleteSettingAsync<PaymobPaymentSettings>();
        await _localizationService.DeleteLocaleResourcesAsync("Plugins.Payments.Paymob");
        await base.UninstallAsync();
    }

    public async Task<string> GetPaymentMethodDescriptionAsync()
        => await _localizationService.GetResourceAsync("Plugins.Payments.Paymob.PaymentMethodDescription");

    public bool SupportCapture => false;
    public bool SupportPartiallyRefund => false;
    public bool SupportRefund => false;
    public bool SupportVoid => false;
    public RecurringPaymentType RecurringPaymentType => RecurringPaymentType.NotSupported;
    public PaymentMethodType PaymentMethodType => PaymentMethodType.Redirection;
    public bool SkipPaymentInfo => true;
}
