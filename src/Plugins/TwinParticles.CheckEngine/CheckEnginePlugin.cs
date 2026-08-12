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
            ["Plugins.TwinParticles.CheckEngine.Garage.Label"] = "Garage",
            ["Plugins.TwinParticles.CheckEngine.Garage.SelectVehicle"] = "Select vehicle",
            ["Plugins.TwinParticles.CheckEngine.Garage.Empty"] = "No vehicle selected",
            ["Plugins.TwinParticles.CheckEngine.Garage.AddVehicle"] = "Add vehicle (VIN)…",
            ["Plugins.TwinParticles.CheckEngine.Garage.VehicleSelector"] = "Choose the vehicle to check parts against",
            ["Plugins.TwinParticles.CheckEngine.Garage.VinPrompt"] = "Enter a VIN to add a vehicle",
            ["Plugins.TwinParticles.CheckEngine.Garage.AddFailed"] = "Unable to add that vehicle.",
            ["Plugins.TwinParticles.CheckEngine.Search.Label"] = "Search parts, OEM number, or VIN",
            ["Plugins.TwinParticles.CheckEngine.Search.Placeholder"] = "Search parts, OEM, or VIN",
            ["Plugins.TwinParticles.CheckEngine.Search.Submit"] = "Search",
            ["Plugins.TwinParticles.CheckEngine.Search.Widen"] = "Include unverified fit",
            ["Plugins.TwinParticles.CheckEngine.Search.Hint"] = "Enter a keyword, an OEM part number, or a 17-character VIN. Results are filtered to your active vehicle unless you include unverified fit.",
            ["Plugins.TwinParticles.CheckEngine.Search.ResultsLabel"] = "Check Engine search results",
            ["Plugins.TwinParticles.CheckEngine.Search.ResultsCount"] = "results",
            ["Plugins.TwinParticles.CheckEngine.Search.Mode"] = "Mode",
            ["Plugins.TwinParticles.CheckEngine.Search.Empty.Title"] = "No matching parts found",
            ["Plugins.TwinParticles.CheckEngine.Search.Empty.Hint"] = "Check the OEM number or VIN, or widen fitment to include unverified parts.",
            ["Plugins.TwinParticles.CheckEngine.Search.Unavailable"] = "Search is unavailable right now.",
            ["Plugins.TwinParticles.CheckEngine.Fitment.Fits"] = "Fits your vehicle",
            ["Plugins.TwinParticles.CheckEngine.Fitment.Fits.Hint"] = "Verified against your active vehicle.",
            ["Plugins.TwinParticles.CheckEngine.Fitment.DoesNotFit"] = "Does not fit",
            ["Plugins.TwinParticles.CheckEngine.Fitment.DoesNotFit.Hint"] = "This part is not compatible with your active vehicle.",
            ["Plugins.TwinParticles.CheckEngine.Fitment.Unknown"] = "Fitment unknown",
            ["Plugins.TwinParticles.CheckEngine.Fitment.Unknown.Hint"] = "We could not verify this part against your vehicle.",
            ["Plugins.TwinParticles.CheckEngine.Fitment.SelectVehicle"] = "Select your vehicle",
            ["Plugins.TwinParticles.CheckEngine.Fitment.SelectVehicle.Hint"] = "Choose a vehicle to check whether this part fits.",
            ["Plugins.TwinParticles.CheckEngine.Fitment.SelectVehicle.Cta"] = "Add your vehicle",
            ["Plugins.TwinParticles.CheckEngine.Hero.Eyebrow"] = "Check Engine",
            ["Plugins.TwinParticles.CheckEngine.Hero.Title"] = "Find the part that fits your car",
            ["Plugins.TwinParticles.CheckEngine.Hero.Lead"] = "Search by VIN, OEM number, or keyword. Save your vehicle once and every result is checked against it.",
            ["Plugins.TwinParticles.CheckEngine.Hero.PrimaryCta"] = "Search parts",
            ["Plugins.TwinParticles.CheckEngine.Hero.SecondaryCta"] = "Add your vehicle",
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

    public override async Task UpdateAsync(string currentVersion, string targetVersion)
    {
        _migrationManager.ApplyUpMigrations(MigrationAssembly, MigrationProcessType.Update);
        await base.UpdateAsync(currentVersion, targetVersion);
    }

    public override async Task UninstallAsync()
    {
        await _permissionService.DeletePermissionAsync(CheckEnginePermissionProvider.ManageCheckEngine.SystemName);
        await _settingService.DeleteSettingAsync<CheckEnginePluginSettings>();
        await _localizationService.DeleteLocaleResourcesAsync("Plugins.TwinParticles.CheckEngine");

        _migrationManager.ApplyDownMigrations(MigrationAssembly);

        await base.UninstallAsync();
    }
}
