using Microsoft.AspNetCore.Mvc;
using Nop.Core;
using Nop.Plugin.Misc.GMaster.Models;
using Nop.Plugin.Misc.GMaster.Services;
using Nop.Services.Configuration;
using Nop.Services.Localization;
using Nop.Services.Messages;
using Nop.Services.Security;
using Nop.Web.Framework;
using Nop.Web.Framework.Controllers;
using Nop.Web.Framework.Mvc.Filters;

namespace Nop.Plugin.Misc.GMaster.Controllers;

[Area(AreaNames.Admin)]
[AuthorizeAdmin]
[AutoValidateAntiforgeryToken]
public sealed class GMasterController : BasePluginController
{
    private readonly GMasterCatalogImportService _catalogImportService;
    private readonly GMasterSettings _settings;
    private readonly ILocalizationService _localizationService;
    private readonly INotificationService _notificationService;
    private readonly IPermissionService _permissionService;
    private readonly ISettingService _settingService;

    public GMasterController(
        GMasterCatalogImportService catalogImportService,
        GMasterSettings settings,
        ILocalizationService localizationService,
        INotificationService notificationService,
        IPermissionService permissionService,
        ISettingService settingService)
    {
        _catalogImportService = catalogImportService;
        _settings = settings;
        _localizationService = localizationService;
        _notificationService = notificationService;
        _permissionService = permissionService;
        _settingService = settingService;
    }

    [HttpGet]
    public async Task<IActionResult> Configure()
    {
        if (!await AuthorizedAsync())
            return AccessDeniedView();

        return View("~/Plugins/Misc.GMaster/Views/Configure.cshtml", BuildModel());
    }

    [HttpPost]
    public async Task<IActionResult> Reimport(ConfigurationModel model, CancellationToken cancellationToken)
    {
        if (!await AuthorizedAsync())
            return AccessDeniedView();

        if (!string.Equals(model.Confirmation?.Trim(), GMasterDefaults.ReimportConfirmation, StringComparison.Ordinal))
        {
            _notificationService.ErrorNotification(
                await _localizationService.GetResourceAsync("Plugins.Misc.GMaster.Reimport.InvalidConfirmation"));
            return RedirectToAction(nameof(Configure));
        }

        // Persist the operator-supplied conversion rate before importing so cost/selling prices use it.
        if (model.RmbToEgpRate > 0)
        {
            _settings.RmbToEgpRate = model.RmbToEgpRate;
            await _settingService.SaveSettingAsync(_settings);
        }

        try
        {
            var result = await _catalogImportService.ReplaceCatalogAsync(cancellationToken);
            var template = await _localizationService.GetResourceAsync("Plugins.Misc.GMaster.Reimport.Success");
            _notificationService.SuccessNotification(string.Format(
                template,
                result.ImportedProducts,
                result.ImportedCategories));
        }
        catch (Exception exception)
        {
            _settings.LastError = exception.Message;
            await _settingService.SaveSettingAsync(_settings);
            _notificationService.ErrorNotification(exception.Message);
        }

        return RedirectToAction(nameof(Configure));
    }

    private ConfigurationModel BuildModel()
        => new()
        {
            LastImportUtc = _settings.LastImportUtc,
            CatalogSourceVersion = _settings.CatalogSourceVersion,
            ImportedProductCount = _settings.ImportedProductCount,
            ImportedCategoryCount = _settings.ImportedCategoryCount,
            ImportedImageCount = _settings.ImportedImageCount,
            RmbToEgpRate = _settings.RmbToEgpRate > 0 ? _settings.RmbToEgpRate : GMasterDefaults.DefaultRmbToEgpRate,
            ClearedProductCount = _settings.ClearedProductCount,
            ClearedCategoryCount = _settings.ClearedCategoryCount,
            ClearedCartItemCount = _settings.ClearedCartItemCount,
            LastError = _settings.LastError
        };

    private async Task<bool> AuthorizedAsync()
        => await _permissionService.AuthorizeAsync(StandardPermissionProvider.ManagePlugins) &&
           await _permissionService.AuthorizeAsync(StandardPermissionProvider.ManageProducts);
}
