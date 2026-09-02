using Nop.Services.Localization;
using Nop.Services.Plugins;
using Nop.Services.Security;
using Nop.Web.Framework.Events;
using Nop.Web.Framework.Menu;
using TwinParticles.CheckEngine.Security;

namespace TwinParticles.CheckEngine.Consumers;

/// <summary>
/// Surfaces Check Engine as a top-level admin sidebar so operators do not have to
/// open Configuration → Local plugins → Configure → Dashboard for every board.
/// Child groups match the dashboard hub (catalog, intelligence, operations, marketplace, portals).
/// </summary>
public sealed class AdminMenuCreatedEventConsumer : BaseAdminMenuCreatedEventConsumer
{
    private readonly ILocalizationService _localizationService;
    private readonly IPermissionService _permissionService;

    public AdminMenuCreatedEventConsumer(
        ILocalizationService localizationService,
        IPermissionService permissionService,
        IPluginManager<IPlugin> pluginManager)
        : base(pluginManager)
    {
        _localizationService = localizationService;
        _permissionService = permissionService;
    }

    protected override string PluginSystemName => "TwinParticles.CheckEngine";

    protected override MenuItemInsertType InsertType => MenuItemInsertType.TryAfterThanBefore;

    protected override string AfterMenuSystemName => "Configuration";

    protected override string BeforeMenuSystemName => "System";

    protected override Task<bool> CheckAccessAsync()
        => _permissionService.AuthorizeAsync(CheckEnginePermissionProvider.ManageCheckEngine.SystemName);

    protected override async Task<AdminMenuItem> GetAdminMenuItemAsync(IPlugin plugin)
    {
        return new AdminMenuItem
        {
            Visible = true,
            SystemName = PluginSystemName,
            Title = await _localizationService.GetResourceAsync("Plugins.TwinParticles.CheckEngine.General"),
            IconClass = "fas fa-car",
            PermissionNames = [CheckEnginePermissionProvider.ManageCheckEngine.SystemName],
            ChildNodes =
            [
                await Item("Dashboard", "Plugins.TwinParticles.CheckEngine.Dashboard", "/Admin/CheckEngine/Dashboard"),
                await Group("Group.Catalog", "Plugins.TwinParticles.CheckEngine.Dashboard.Group.Catalog",
                    await Item("VehicleAdmin", "Plugins.TwinParticles.CheckEngine.Vehicle.Admin", "/Admin/CheckEngine/VehicleAdmin/Index"),
                    await Item("OemAdmin", "Plugins.TwinParticles.CheckEngine.Oem.Admin", "/Admin/CheckEngine/OemAdmin/Index"),
                    await Item("FitmentAdmin", "Plugins.TwinParticles.CheckEngine.Fitment.ClaimsReview", "/Admin/CheckEngine/FitmentAdmin/ClaimsReview"),
                    await Item("ImportAdmin", "Plugins.TwinParticles.CheckEngine.Import.Batch", "/Admin/CheckEngine/ImportAdmin/BatchBoard"),
                    await Item("ImageAdmin", "Plugins.TwinParticles.CheckEngine.Image.Admin", "/Admin/CheckEngine/ImageAdmin/Index"),
                    await Item("ReferenceDataAdmin", "Plugins.TwinParticles.CheckEngine.ReferenceData.Admin", "/Admin/CheckEngine/ReferenceDataAdmin/Index")),
                await Group("Group.Intelligence", "Plugins.TwinParticles.CheckEngine.Dashboard.Group.Intelligence",
                    await Item("SearchAdmin", "Plugins.TwinParticles.CheckEngine.Search.Admin", "/Admin/CheckEngine/SearchAdmin/Index"),
                    await Item("SeoAdmin", "Plugins.TwinParticles.CheckEngine.Seo.Admin", "/Admin/CheckEngine/SeoAdmin/Index"),
                    await Item("AiDashboard", "Plugins.TwinParticles.CheckEngine.Ai.Dashboard", "/Admin/CheckEngine/AiAdmin/Dashboard"),
                    await Item("AiReview", "Plugins.TwinParticles.CheckEngine.Ai.Review", "/Admin/CheckEngine/AiAdmin/ReviewBoard"),
                    await Item("GlossaryAdmin", "Plugins.TwinParticles.CheckEngine.Glossary.Admin", "/Admin/CheckEngine/AiAdmin/GlossaryBoard"),
                    await Item("SpecKeysAdmin", "Plugins.TwinParticles.CheckEngine.SpecKeys.Admin", "/Admin/CheckEngine/AiAdmin/SpecKeysBoard"),
                    await Item("FitmentAiReview", "Plugins.TwinParticles.CheckEngine.Fitment.AiReview", "/Admin/CheckEngine/FitmentAdmin/ReviewBoard")),
                await Group("Group.Operations", "Plugins.TwinParticles.CheckEngine.Dashboard.Group.Operations",
                    await Item("GarageAdmin", "Plugins.TwinParticles.CheckEngine.Garage.Admin", "/Admin/CheckEngine/GarageAdmin/Index"),
                    await Item("ErpAdmin", "Plugins.TwinParticles.CheckEngine.Erp.Admin", "/Admin/CheckEngine/ErpAdmin/Index"),
                    await Item("DiagnosticsAdmin", "Plugins.TwinParticles.CheckEngine.Diagnostics.Admin", "/Admin/CheckEngine/DiagnosticsAdmin/Index")),
                await Group("Group.Marketplace", "Plugins.TwinParticles.CheckEngine.Dashboard.Group.Marketplace",
                    await Item("VendorAdmin", "Plugins.TwinParticles.CheckEngine.Marketplace.Review", "/Admin/CheckEngine/VendorAdmin/ReviewBoard"),
                    await Item("VendorScoreboard", "Plugins.TwinParticles.CheckEngine.Marketplace.Scoreboard", "/Admin/CheckEngine/VendorAdmin/Scoreboard"),
                    await Item("CommissionAdmin", "Plugins.TwinParticles.CheckEngine.Commission.Configure", "/Admin/CheckEngine/CommissionAdmin/Configure"),
                    await Item("PayoutAdmin", "Plugins.TwinParticles.CheckEngine.Payout.Admin", "/Admin/CheckEngine/PayoutAdmin/Index")),
                await Group("Group.Portals", "Plugins.TwinParticles.CheckEngine.Dashboard.Group.Portals",
                    await Item("PortalAdmin", "Plugins.TwinParticles.CheckEngine.Portal.Admin.Title", "/Admin/CheckEngine/PortalAdmin/Accounts")),
                await Item("Configure", "Plugins.TwinParticles.CheckEngine.Configuration", "/Admin/CheckEngine/Configure")
            ]
        };
    }

    private async Task<AdminMenuItem> Group(string systemSuffix, string resourceKey, params AdminMenuItem[] children)
        => new()
        {
            Visible = true,
            SystemName = PluginSystemName + "." + systemSuffix,
            Title = await _localizationService.GetResourceAsync(resourceKey),
            IconClass = "far fa-folder",
            PermissionNames = [CheckEnginePermissionProvider.ManageCheckEngine.SystemName],
            ChildNodes = children
        };

    private async Task<AdminMenuItem> Item(string systemSuffix, string resourceKey, string url)
        => new()
        {
            Visible = true,
            SystemName = PluginSystemName + "." + systemSuffix,
            Title = await _localizationService.GetResourceAsync(resourceKey),
            Url = url,
            IconClass = "far fa-dot-circle"
        };
}
