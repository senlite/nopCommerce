using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Nop.Core;
using Nop.Data.Migrations;
using Nop.Services.Cms;
using Nop.Services.Common;
using Nop.Services.Configuration;
using Nop.Services.Localization;
using Nop.Services.Plugins;
using Nop.Services.ScheduleTasks;
using Nop.Services.Security;
using Nop.Web.Framework.Infrastructure;
using TwinParticles.CheckEngine.Configuration;
using TwinParticles.CheckEngine.Security;

namespace TwinParticles.CheckEngine;

public sealed class CheckEnginePlugin : BasePlugin, IMiscPlugin, IWidgetPlugin
{
    private readonly ILocalizationService _localizationService;
    private readonly IMigrationManager _migrationManager;
    private readonly IPermissionService _permissionService;
    private readonly ISettingService _settingService;
    private readonly IWebHelper _webHelper;

    public CheckEnginePlugin(ILocalizationService localizationService,
        IMigrationManager migrationManager,
        IPermissionService permissionService,
        ISettingService settingService,
        IWebHelper webHelper)
    {
        _localizationService = localizationService;
        _migrationManager = migrationManager;
        _permissionService = permissionService;
        _settingService = settingService;
        _webHelper = webHelper;
    }

    /// <summary>
    /// Check Engine migrations live in the Infrastructure assembly, but nopCommerce only scans the
    /// assembly declaring the plugin type, so they must be applied explicitly.
    /// </summary>
    private static Assembly MigrationAssembly =>
        typeof(Infrastructure.DependencyInjection.ServiceCollectionExtensions).Assembly;

    public override string GetConfigurationPageUrl()
    {
        return $"{_webHelper.GetStoreLocation()}Admin/CheckEngine/Configure";
    }

    public Task<IList<string>> GetWidgetZonesAsync()
    {
        return Task.FromResult<IList<string>>([
            PublicWidgetZones.HeaderAfter,
            PublicWidgetZones.HeaderMenuAfter,
            PublicWidgetZones.BodyStartHtmlTagAfter,
            PublicWidgetZones.ProductDetailsTop,
            PublicWidgetZones.HomepageTop
        ]);
    }

    public Type GetWidgetViewComponent(string widgetZone)
    {
        return typeof(Components.CheckEngineThemeChromeViewComponent);
    }

    public bool HideInWidgetList => false;

    public override async Task InstallAsync()
    {
        _migrationManager.ApplyUpMigrations(MigrationAssembly, MigrationProcessType.Installation);

        await _settingService.SaveSettingAsync(new CheckEnginePluginSettings());
        await _permissionService.InstallPermissionsAsync(new CheckEnginePermissionProvider());

        await _localizationService.AddOrUpdateLocaleResourceAsync(new Dictionary<string, string>
        {
            ["Plugins.TwinParticles.CheckEngine.General"] = "Check Engine",
            ["Plugins.TwinParticles.CheckEngine.General.Enabled"] = "Enabled",
            ["Plugins.TwinParticles.CheckEngine.General.Enabled.Hint"] = "Determines whether Check Engine core services are enabled.",
            ["Plugins.TwinParticles.CheckEngine.Configuration"] = "Configuration",
            ["Plugins.TwinParticles.CheckEngine.Configuration.Fields.Enabled"] = "Enabled",
            ["Plugins.TwinParticles.CheckEngine.Configuration.Fields.Enabled.Hint"] = "Toggle to enable or disable the Check Engine plugin scaffold.",
            ["Plugins.TwinParticles.CheckEngine.Dashboard"] = "Check Engine Dashboard",
            ["Plugins.TwinParticles.CheckEngine.Dashboard.AdminLinks"] = "Admin JSON endpoints",
            ["Plugins.TwinParticles.CheckEngine.Dashboard.ImportUpload"] = "Import upload",
            ["Plugins.TwinParticles.CheckEngine.Dashboard.ImportUpload.Hint"] = "Select a supplier file and run the import pipeline (content is sent as Base64 JSON).",
            ["Plugins.TwinParticles.CheckEngine.L10n.Number"] = "Number format",
            ["Plugins.TwinParticles.CheckEngine.L10n.Date"] = "Date format",
            ["Plugins.TwinParticles.CheckEngine.L10n.Unit"] = "Unit format",
            ["Plugins.TwinParticles.CheckEngine.L10n.Preview"] = "Localization preview",
            ["Plugins.TwinParticles.CheckEngine.Licence.Status"] = "Licence status",
            ["Plugins.TwinParticles.CheckEngine.Licence.LastHeartbeat"] = "Last heartbeat",
            ["Plugins.TwinParticles.CheckEngine.Licence.ActivationKey"] = "Activation key"
        });

        await base.InstallAsync();
    }

    public override async Task UninstallAsync()
    {
        await _permissionService.UninstallPermissionsAsync(new CheckEnginePermissionProvider());
        await _settingService.DeleteSettingAsync<CheckEnginePluginSettings>();
        await _localizationService.DeleteLocaleResourcesAsync("Plugins.TwinParticles.CheckEngine");

        _migrationManager.ApplyDownMigrations(MigrationAssembly);

        await base.UninstallAsync();
    }
}
