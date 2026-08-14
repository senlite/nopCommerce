using Microsoft.AspNetCore.Mvc;
using Nop.Plugin.Shipping.Bosta.Models;
using Nop.Services.Configuration;
using Nop.Services.Localization;
using Nop.Services.Messages;
using Nop.Services.Security;
using Nop.Web.Framework;
using Nop.Web.Framework.Controllers;
using Nop.Web.Framework.Mvc.Filters;

namespace Nop.Plugin.Shipping.Bosta.Controllers;

[AuthorizeAdmin]
[Area(AreaNames.ADMIN)]
[AutoValidateAntiforgeryToken]
public class ShippingBostaController : BasePluginController
{
    private readonly ILocalizationService _localizationService;
    private readonly INotificationService _notificationService;
    private readonly ISettingService _settingService;

    public ShippingBostaController(
        ILocalizationService localizationService,
        INotificationService notificationService,
        ISettingService settingService)
    {
        _localizationService = localizationService;
        _notificationService = notificationService;
        _settingService = settingService;
    }

    [CheckPermission(StandardPermission.Configuration.MANAGE_SHIPPING_SETTINGS)]
    public async Task<IActionResult> Configure()
    {
        var settings = await _settingService.LoadSettingAsync<BostaShippingSettings>();
        var model = new ConfigurationModel
        {
            ApiKey = settings.ApiKey,
            OptionName = settings.OptionName,
            UseSandbox = settings.UseSandbox
        };

        return View("~/Plugins/Shipping.Bosta/Views/Configure.cshtml", model);
    }

    [HttpPost]
    [CheckPermission(StandardPermission.Configuration.MANAGE_SHIPPING_SETTINGS)]
    public async Task<IActionResult> Configure(ConfigurationModel model)
    {
        if (!ModelState.IsValid)
            return await Configure();

        var settings = await _settingService.LoadSettingAsync<BostaShippingSettings>();
        settings.ApiKey = model.ApiKey;
        settings.OptionName = model.OptionName;
        settings.UseSandbox = model.UseSandbox;

        await _settingService.SaveSettingAsync(settings);
        await _settingService.ClearCacheAsync();

        _notificationService.SuccessNotification(await _localizationService.GetResourceAsync("Admin.Plugins.Saved"));

        return await Configure();
    }
}
