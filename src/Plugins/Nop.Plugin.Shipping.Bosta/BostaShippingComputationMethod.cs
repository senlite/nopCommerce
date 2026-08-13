using Nop.Core;
using Nop.Core.Domain.Shipping;
using Nop.Plugin.Shipping.Bosta.Services;
using Nop.Services.Configuration;
using Nop.Services.Localization;
using Nop.Services.Plugins;
using Nop.Services.Shipping;
using Nop.Services.Shipping.Tracking;

namespace Nop.Plugin.Shipping.Bosta;

/// <summary>
/// Reference Bosta domestic shipping rate computation for the Egyptian market. Uses only nopCommerce
/// shipping abstractions and has zero dependency on the Check Engine core plugin (EP-17, FR-950/951).
/// </summary>
public class BostaShippingComputationMethod : BasePlugin, IShippingRateComputationMethod
{
    private readonly BostaShippingSettings _settings;
    private readonly BostaRateCalculator _rateCalculator;
    private readonly ILocalizationService _localizationService;
    private readonly ISettingService _settingService;
    private readonly IShippingService _shippingService;
    private readonly IWebHelper _webHelper;

    public BostaShippingComputationMethod(
        BostaShippingSettings settings,
        BostaRateCalculator rateCalculator,
        ILocalizationService localizationService,
        ISettingService settingService,
        IShippingService shippingService,
        IWebHelper webHelper)
    {
        _settings = settings;
        _rateCalculator = rateCalculator;
        _localizationService = localizationService;
        _settingService = settingService;
        _shippingService = shippingService;
        _webHelper = webHelper;
    }

    public async Task<GetShippingOptionResponse> GetShippingOptionsAsync(GetShippingOptionRequest getShippingOptionRequest)
    {
        ArgumentNullException.ThrowIfNull(getShippingOptionRequest);

        var response = new GetShippingOptionResponse();

        if (getShippingOptionRequest.Items is null || getShippingOptionRequest.Items.Count == 0)
        {
            response.AddError("No shipment items");
            return response;
        }

        var zone = getShippingOptionRequest.ShippingAddress?.City;
        var totalWeightKg = await _shippingService.GetTotalWeightAsync(getShippingOptionRequest);
        var rate = _rateCalculator.CalculateRate(zone, totalWeightKg);

        response.ShippingOptions.Add(new ShippingOption
        {
            Name = _settings.OptionName,
            Description = "Bosta domestic delivery",
            Rate = rate
        });

        return response;
    }

    public async Task<decimal?> GetFixedRateAsync(GetShippingOptionRequest getShippingOptionRequest)
    {
        // Rate depends on destination zone and weight, so there is no single fixed rate.
        await Task.CompletedTask;
        return null;
    }

    public Task<IShipmentTracker> GetShipmentTrackerAsync()
        => Task.FromResult<IShipmentTracker>(new BostaTracker());

    public override string GetConfigurationPageUrl()
        => $"{_webHelper.GetStoreLocation()}Admin/ShippingBosta/Configure";

    public override async Task InstallAsync()
    {
        await _settingService.SaveSettingAsync(new BostaShippingSettings { OptionName = "Bosta", UseSandbox = true });

        await _localizationService.AddOrUpdateLocaleResourceAsync(new Dictionary<string, string>
        {
            ["Plugins.Shipping.Bosta.Fields.ApiKey"] = "API key",
            ["Plugins.Shipping.Bosta.Fields.ApiKey.Hint"] = "Your Bosta API key (kept as a secret; never committed).",
            ["Plugins.Shipping.Bosta.Fields.OptionName"] = "Checkout option name",
            ["Plugins.Shipping.Bosta.Fields.OptionName.Hint"] = "The name shown to customers at checkout.",
            ["Plugins.Shipping.Bosta.Fields.UseSandbox"] = "Use sandbox",
            ["Plugins.Shipping.Bosta.Fields.UseSandbox.Hint"] = "When enabled, rates are computed locally and no Bosta API call is made."
        });

        await base.InstallAsync();
    }

    public override async Task UninstallAsync()
    {
        await _settingService.DeleteSettingAsync<BostaShippingSettings>();
        await _localizationService.DeleteLocaleResourcesAsync("Plugins.Shipping.Bosta");
        await base.UninstallAsync();
    }
}
