using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Nop.Core;
using Nop.Core.Domain.ScheduleTasks;
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
    private readonly IScheduleTaskService _scheduleTaskService;
    private readonly ISettingService _settingService;
    private readonly IWebHelper _webHelper;

    public CheckEnginePlugin(ILocalizationService localizationService,
        IMigrationManager migrationManager,
        IPermissionService permissionService,
        IScheduleTaskService scheduleTaskService,
        ISettingService settingService,
        IWebHelper webHelper)
    {
        _localizationService = localizationService;
        _migrationManager = migrationManager;
        _permissionService = permissionService;
        _scheduleTaskService = scheduleTaskService;
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
        await EnsureScheduleTasksAsync();

        await AddOrUpdateLocaleResourcesAsync();

        await base.InstallAsync();
    }

    /// <summary>
    /// Applies the plugin's locale resources. Run on update as well as install, otherwise strings
    /// added by a release only exist on stores that installed the plugin fresh, and upgraded stores
    /// render raw resource keys.
    /// </summary>
    private Task AddOrUpdateLocaleResourcesAsync()
    {
        return _localizationService.AddOrUpdateLocaleResourceAsync(new Dictionary<string, string>
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
            ["Plugins.TwinParticles.CheckEngine.Search.Facets.Title"] = "Refine",
            ["Plugins.TwinParticles.CheckEngine.Search.Facets.Category"] = "Category",
            ["Plugins.TwinParticles.CheckEngine.Search.Facets.Brand"] = "Brand",
            ["Plugins.TwinParticles.CheckEngine.Search.Facets.Price"] = "Price",
            ["Plugins.TwinParticles.CheckEngine.Search.Facets.Fitment"] = "Fitment",
            ["Plugins.TwinParticles.CheckEngine.Search.Facets.Clear"] = "Clear filters",
            ["Plugins.TwinParticles.CheckEngine.Search.Recovery.Title"] = "Try one of these",
            ["Plugins.TwinParticles.CheckEngine.Search.Suggest.Vehicles"] = "Vehicles",
            ["Plugins.TwinParticles.CheckEngine.Search.Suggest.Oems"] = "OEM numbers",
            ["Plugins.TwinParticles.CheckEngine.Search.Suggest.Products"] = "Products",
            ["Plugins.TwinParticles.CheckEngine.Menu.AriaLabel"] = "Parts navigation",
            ["Plugins.TwinParticles.CheckEngine.Menu.AllParts"] = "All parts",
            ["Plugins.TwinParticles.CheckEngine.Menu.Eyebrow"] = "Parts catalog",
            ["Plugins.TwinParticles.CheckEngine.Menu.Title"] = "Browse by category",
            ["Plugins.TwinParticles.CheckEngine.Menu.Hint"] = "Choose a category, or add your vehicle to see verified-fit parts first.",
            ["Plugins.TwinParticles.CheckEngine.Menu.Empty"] = "Categories will appear here when the catalog is published.",
            ["Plugins.TwinParticles.CheckEngine.Menu.AddVehicle"] = "Add your vehicle",
            ["Plugins.TwinParticles.CheckEngine.Menu.SearchByOem"] = "Search by OEM number",
            ["Plugins.TwinParticles.CheckEngine.Fitment.Fits"] = "Fits your vehicle",
            ["Plugins.TwinParticles.CheckEngine.Fitment.Fits.Hint"] = "Verified against your active vehicle.",
            ["Plugins.TwinParticles.CheckEngine.Fitment.DoesNotFit"] = "Does not fit",
            ["Plugins.TwinParticles.CheckEngine.Fitment.DoesNotFit.Hint"] = "This part is not compatible with your active vehicle.",
            ["Plugins.TwinParticles.CheckEngine.Fitment.Unknown"] = "Fitment unknown",
            ["Plugins.TwinParticles.CheckEngine.Fitment.Unknown.Hint"] = "We could not verify this part against your vehicle.",
            ["Plugins.TwinParticles.CheckEngine.Fitment.NeedsDetail"] = "More vehicle detail needed",
            ["Plugins.TwinParticles.CheckEngine.Fitment.NeedsDetail.Hint"] = "This part fits some versions of your vehicle. Add the missing details to confirm.",
            ["Plugins.TwinParticles.CheckEngine.Fitment.NeedsDetail.Cta"] = "Complete vehicle details",
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
    }

    public override async Task UpdateAsync(string currentVersion, string targetVersion)
    {
        // Apply every pending migration, not only Update-typed ones. All Check Engine migrations are
        // tagged Installation (per convention), and ApplyUpMigrations(..., Update) filters those out,
        // so a version bump that ships new schema would otherwise apply nothing. NoMatter runs all
        // unapplied migrations regardless of process type.
        _migrationManager.ApplyUpMigrations(MigrationAssembly, MigrationProcessType.NoMatter);
        await EnsureScheduleTasksAsync();
        await AddOrUpdateLocaleResourcesAsync();
        await base.UpdateAsync(currentVersion, targetVersion);
    }

    public override async Task UninstallAsync()
    {
        var settings = await _settingService.LoadSettingAsync<CheckEnginePluginSettings>();
        if (!settings.UninstallExportPreparedUtc.HasValue ||
            DateTime.UtcNow - settings.UninstallExportPreparedUtc.Value > TimeSpan.FromHours(24))
        {
            throw new InvalidOperationException(
                "Check Engine uninstall blocked: download a fresh export from " +
                "/Admin/CheckEngine/UninstallAdmin/Export before retrying. The export authorization expires after 24 hours.");
        }

        var erpTask = await _scheduleTaskService.GetTaskByTypeAsync(typeof(Tasks.ErpSyncQueueTask).FullName!);
        if (erpTask is not null)
            await _scheduleTaskService.DeleteTaskAsync(erpTask);
        var auditTask = await _scheduleTaskService.GetTaskByTypeAsync(typeof(Tasks.AuditRetentionTask).FullName!);
        if (auditTask is not null)
            await _scheduleTaskService.DeleteTaskAsync(auditTask);
        var licenceTask = await _scheduleTaskService.GetTaskByTypeAsync(typeof(Tasks.LicenceHeartbeatTask).FullName!);
        if (licenceTask is not null)
            await _scheduleTaskService.DeleteTaskAsync(licenceTask);
        var reconciliationTask = await _scheduleTaskService.GetTaskByTypeAsync(typeof(Tasks.ErpReconciliationTask).FullName!);
        if (reconciliationTask is not null)
            await _scheduleTaskService.DeleteTaskAsync(reconciliationTask);

        await _permissionService.DeletePermissionAsync(CheckEnginePermissionProvider.ManageCheckEngine.SystemName);
        await _settingService.DeleteSettingAsync<CheckEnginePluginSettings>();
        await _localizationService.DeleteLocaleResourcesAsync("Plugins.TwinParticles.CheckEngine");

        _migrationManager.ApplyDownMigrations(MigrationAssembly);

        await base.UninstallAsync();
    }

    private async Task EnsureScheduleTasksAsync()
    {
        await EnsureScheduleTaskAsync(
            typeof(Tasks.ErpSyncQueueTask).FullName!,
            "Check Engine ERP synchronization queue",
            5 * 60);
        await EnsureScheduleTaskAsync(
            typeof(Tasks.AuditRetentionTask).FullName!,
            "Check Engine audit retention",
            24 * 60 * 60);
        await EnsureScheduleTaskAsync(
            typeof(Tasks.LicenceHeartbeatTask).FullName!,
            "Check Engine licence heartbeat",
            24 * 60 * 60);
        await EnsureScheduleTaskAsync(
            typeof(Tasks.ErpReconciliationTask).FullName!,
            "Check Engine ERP reconciliation",
            24 * 60 * 60);
    }

    private async Task EnsureScheduleTaskAsync(string type, string name, int seconds)
    {
        if (await _scheduleTaskService.GetTaskByTypeAsync(type) is not null)
            return;

        await _scheduleTaskService.InsertTaskAsync(new ScheduleTask
        {
            Name = name,
            Type = type,
            Seconds = seconds,
            Enabled = true,
            LastEnabledUtc = DateTime.UtcNow
        });
    }
}
