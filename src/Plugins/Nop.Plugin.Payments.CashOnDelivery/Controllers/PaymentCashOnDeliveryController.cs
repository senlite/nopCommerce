using Microsoft.AspNetCore.Mvc;
using Nop.Core;
using Nop.Plugin.Payments.CashOnDelivery.Models;
using Nop.Services.Configuration;
using Nop.Services.Localization;
using Nop.Services.Messages;
using Nop.Services.Security;
using Nop.Web.Framework;
using Nop.Web.Framework.Controllers;
using Nop.Web.Framework.Mvc.Filters;

namespace Nop.Plugin.Payments.CashOnDelivery.Controllers;

[AuthorizeAdmin]
[Area(AreaNames.ADMIN)]
[AutoValidateAntiforgeryToken]
public class PaymentCashOnDeliveryController : BasePaymentController
{
    #region Fields

    protected readonly ILanguageService _languageService;
    protected readonly ILocalizationService _localizationService;
    protected readonly INotificationService _notificationService;
    protected readonly IPermissionService _permissionService;
    protected readonly ISettingService _settingService;
    protected readonly IStoreContext _storeContext;

    #endregion

    #region Ctor

    public PaymentCashOnDeliveryController(ILanguageService languageService,
        ILocalizationService localizationService,
        INotificationService notificationService,
        IPermissionService permissionService,
        ISettingService settingService,
        IStoreContext storeContext)
    {
        _languageService = languageService;
        _localizationService = localizationService;
        _notificationService = notificationService;
        _permissionService = permissionService;
        _settingService = settingService;
        _storeContext = storeContext;
    }

    #endregion

    #region Methods

    public async Task<IActionResult> Configure()
    {
        if (!await _permissionService.AuthorizeAsync(StandardPermissionProvider.ManagePaymentMethods))
            return AccessDeniedView();

        //load settings for a chosen store scope
        var storeScope = await _storeContext.GetActiveStoreScopeConfigurationAsync();
        var CashOnDeliveryPaymentSettings = await _settingService.LoadSettingAsync<CashOnDeliveryPaymentSettings>(storeScope);

        var model = new ConfigurationModel
        {
            DescriptionText = CashOnDeliveryPaymentSettings.DescriptionText
        };

        //locales
        await AddLocalesAsync(_languageService, model.Locales, async (locale, languageId) =>
        {
            locale.DescriptionText = await _localizationService
                .GetLocalizedSettingAsync(CashOnDeliveryPaymentSettings, x => x.DescriptionText, languageId, 0, false, false);
        });
        model.AdditionalFee = CashOnDeliveryPaymentSettings.AdditionalFee;
        model.AdditionalFeePercentage = CashOnDeliveryPaymentSettings.AdditionalFeePercentage;
        model.ShippableProductRequired = CashOnDeliveryPaymentSettings.ShippableProductRequired;

        model.ActiveStoreScopeConfiguration = storeScope;
        if (storeScope > 0)
        {
            model.DescriptionText_OverrideForStore = await _settingService.SettingExistsAsync(CashOnDeliveryPaymentSettings, x => x.DescriptionText, storeScope);
            model.AdditionalFee_OverrideForStore = await _settingService.SettingExistsAsync(CashOnDeliveryPaymentSettings, x => x.AdditionalFee, storeScope);
            model.AdditionalFeePercentage_OverrideForStore = await _settingService.SettingExistsAsync(CashOnDeliveryPaymentSettings, x => x.AdditionalFeePercentage, storeScope);
            model.ShippableProductRequired_OverrideForStore = await _settingService.SettingExistsAsync(CashOnDeliveryPaymentSettings, x => x.ShippableProductRequired, storeScope);
        }

        return View("~/Plugins/Payments.CashOnDelivery/Views/Configure.cshtml", model);
    }

    [HttpPost]
    public async Task<IActionResult> Configure(ConfigurationModel model)
    {
        if (!await _permissionService.AuthorizeAsync(StandardPermissionProvider.ManagePaymentMethods))
            return AccessDeniedView();

        if (!ModelState.IsValid)
            return await Configure();

        //load settings for a chosen store scope
        var storeScope = await _storeContext.GetActiveStoreScopeConfigurationAsync();
        var CashOnDeliveryPaymentSettings = await _settingService.LoadSettingAsync<CashOnDeliveryPaymentSettings>(storeScope);

        //save settings
        CashOnDeliveryPaymentSettings.DescriptionText = model.DescriptionText;
        CashOnDeliveryPaymentSettings.AdditionalFee = model.AdditionalFee;
        CashOnDeliveryPaymentSettings.AdditionalFeePercentage = model.AdditionalFeePercentage;
        CashOnDeliveryPaymentSettings.ShippableProductRequired = model.ShippableProductRequired;

        /* We do not clear cache after each setting update.
         * This behavior can increase performance because cached settings will not be cleared 
         * and loaded from database after each update */
        await _settingService.SaveSettingOverridablePerStoreAsync(CashOnDeliveryPaymentSettings, x => x.DescriptionText, model.DescriptionText_OverrideForStore, storeScope, false);
        await _settingService.SaveSettingOverridablePerStoreAsync(CashOnDeliveryPaymentSettings, x => x.AdditionalFee, model.AdditionalFee_OverrideForStore, storeScope, false);
        await _settingService.SaveSettingOverridablePerStoreAsync(CashOnDeliveryPaymentSettings, x => x.AdditionalFeePercentage, model.AdditionalFeePercentage_OverrideForStore, storeScope, false);
        await _settingService.SaveSettingOverridablePerStoreAsync(CashOnDeliveryPaymentSettings, x => x.ShippableProductRequired, model.ShippableProductRequired_OverrideForStore, storeScope, false);

        //now clear settings cache
        await _settingService.ClearCacheAsync();

        //localization. no multi-store support for localization yet.
        foreach (var localized in model.Locales)
        {
            await _localizationService.SaveLocalizedSettingAsync(CashOnDeliveryPaymentSettings,
                x => x.DescriptionText, localized.LanguageId, localized.DescriptionText);
        }

        _notificationService.SuccessNotification(await _localizationService.GetResourceAsync("Admin.Plugins.Saved"));

        return await Configure();
    }

    #endregion
}