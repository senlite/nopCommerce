using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Nop.Services.Configuration;
using Nop.Services.Localization;
using Nop.Services.Security;
using Nop.Web.Framework.Components;
using Nop.Web.Framework.Infrastructure;
using TwinParticles.CheckEngine.Configuration;
using TwinParticles.CheckEngine.Models;
using TwinParticles.CheckEngine.Security;

namespace TwinParticles.CheckEngine.Components;

public sealed class UninstallPreparationViewComponent : NopViewComponent
{
    private static readonly TimeSpan PreparationLifetime = TimeSpan.FromHours(24);

    private readonly ILocalizationService _localizationService;
    private readonly IPermissionService _permissionService;
    private readonly ISettingService _settingService;

    public UninstallPreparationViewComponent(
        ILocalizationService localizationService,
        IPermissionService permissionService,
        ISettingService settingService)
    {
        _localizationService = localizationService;
        _permissionService = permissionService;
        _settingService = settingService;
    }

    public async Task<IViewComponentResult> InvokeAsync(string widgetZone, object? additionalData)
    {
        if (!await _permissionService.AuthorizeAsync(CheckEnginePermissionProvider.ManageCheckEngine.SystemName))
            return Content(string.Empty);

        var compact = string.Equals(widgetZone, AdminWidgetZones.PluginListButtons, StringComparison.OrdinalIgnoreCase);
        if (!compact && !string.IsNullOrWhiteSpace(widgetZone))
            return Content(string.Empty);

        var settings = await _settingService.LoadSettingAsync<CheckEnginePluginSettings>();
        var preparedUtc = settings.UninstallExportPreparedUtc;
        var prepared = preparedUtc.HasValue && DateTime.UtcNow - preparedUtc.Value <= PreparationLifetime;

        var model = new UninstallPreparationModel
        {
            Compact = compact,
            InterceptPluginListUninstall = compact,
            Warning = await ResourceAsync("Plugins.TwinParticles.CheckEngine.Uninstall.Warning"),
            ExportRequired = await ResourceAsync("Plugins.TwinParticles.CheckEngine.Uninstall.ExportRequired"),
            ExportPrepared = await ResourceAsync("Plugins.TwinParticles.CheckEngine.Uninstall.ExportPrepared"),
            ExportExpired = await ResourceAsync("Plugins.TwinParticles.CheckEngine.Uninstall.ExportExpired"),
            ExportAction = await ResourceAsync("Plugins.TwinParticles.CheckEngine.Uninstall.ExportAction"),
            ConfirmTitle = await ResourceAsync("Plugins.TwinParticles.CheckEngine.Uninstall.ConfirmTitle"),
            ConfirmBody = await ResourceAsync("Plugins.TwinParticles.CheckEngine.Uninstall.ConfirmBody"),
            ConfirmProceed = await ResourceAsync("Plugins.TwinParticles.CheckEngine.Uninstall.ConfirmProceed"),
            ConfirmCancel = await ResourceAsync("Plugins.TwinParticles.CheckEngine.Uninstall.ConfirmCancel"),
            ExportUrl = "/Admin/CheckEngine/UninstallAdmin/Export",
            Prepared = prepared,
            PreparedUtc = preparedUtc,
            ExpiresUtc = preparedUtc?.Add(PreparationLifetime)
        };

        return View("~/Plugins/TwinParticles.CheckEngine/Views/Shared/Components/UninstallPreparation/Default.cshtml", model);
    }

    private async Task<string> ResourceAsync(string key)
    {
        var value = await _localizationService.GetResourceAsync(key);
        return string.IsNullOrWhiteSpace(value) || string.Equals(value, key, StringComparison.OrdinalIgnoreCase)
            ? key
            : value;
    }
}
