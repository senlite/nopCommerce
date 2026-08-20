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
                await Item("VehicleAdmin", "Plugins.TwinParticles.CheckEngine.Vehicle.Admin", "/Admin/CheckEngine/VehicleAdmin/Index"),
                await Item("ImportAdmin", "Plugins.TwinParticles.CheckEngine.Import.Batch", "/Admin/CheckEngine/ImportAdmin/BatchBoard"),
                await Item("ImageAdmin", "Plugins.TwinParticles.CheckEngine.Image.Admin", "/Admin/CheckEngine/ImageAdmin/Index"),
                await Item("FitmentAdmin", "Plugins.TwinParticles.CheckEngine.Fitment.ClaimsReview", "/Admin/CheckEngine/FitmentAdmin/ClaimsReview"),
                await Item("SearchAdmin", "Plugins.TwinParticles.CheckEngine.Search.Admin", "/Admin/CheckEngine/SearchAdmin/Index"),
                await Item("VendorAdmin", "Plugins.TwinParticles.CheckEngine.Marketplace.Review", "/Admin/CheckEngine/VendorAdmin/ReviewBoard"),
                await Item("PortalAdmin", "Plugins.TwinParticles.CheckEngine.Portal.Admin.Title", "/Admin/CheckEngine/PortalAdmin/Accounts"),
                await Item("Configure", "Plugins.TwinParticles.CheckEngine.Configuration", "/Admin/CheckEngine/Configure")
            ]
        };
    }

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
