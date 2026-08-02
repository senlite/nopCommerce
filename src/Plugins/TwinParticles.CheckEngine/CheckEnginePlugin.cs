using System.Collections.Generic;
using System.Threading.Tasks;
using Nop.Core;
using Nop.Services.Configuration;
using Nop.Services.Localization;
using Nop.Services.Plugins;
using Nop.Services.Security;
using TwinParticles.CheckEngine.Configuration;
using TwinParticles.CheckEngine.Security;

namespace TwinParticles.CheckEngine;

public sealed class CheckEnginePlugin : BasePlugin, IMiscPlugin
{
    private readonly ILocalizationService _localizationService;
    private readonly IPermissionService _permissionService;
    private readonly ISettingService _settingService;
    private readonly IWebHelper _webHelper;

    public CheckEnginePlugin(ILocalizationService localizationService,
        IPermissionService permissionService,
        ISettingService settingService,
        IWebHelper webHelper)
    {
        _localizationService = localizationService;
        _permissionService = permissionService;
        _settingService = settingService;
        _webHelper = webHelper;
    }

    public override string GetConfigurationPageUrl()
    {
        return $"{_webHelper.GetStoreLocation()}Admin/CheckEngine/Configure";
    }

    public override async Task InstallAsync()
    {
        await _settingService.SaveSettingAsync(new CheckEnginePluginSettings());
        await _permissionService.InstallPermissionsAsync(new CheckEnginePermissionProvider());

        await _localizationService.AddOrUpdateLocaleResourceAsync(new Dictionary<string, string>
        {
            ["Plugins.TwinParticles.CheckEngine.General"] = "Check Engine",
            ["Plugins.TwinParticles.CheckEngine.General.Enabled"] = "Enabled",
            ["Plugins.TwinParticles.CheckEngine.General.Enabled.Hint"] = "Determines whether Check Engine core services are enabled.",
            ["Plugins.TwinParticles.CheckEngine.Configuration"] = "Configuration",
            ["Plugins.TwinParticles.CheckEngine.Configuration.Fields.Enabled"] = "Enabled",
            ["Plugins.TwinParticles.CheckEngine.Configuration.Fields.Enabled.Hint"] = "Toggle to enable or disable the Check Engine plugin scaffold."
        });

        await base.InstallAsync();
    }

    public override async Task UninstallAsync()
    {
        await _permissionService.UninstallPermissionsAsync(new CheckEnginePermissionProvider());
        await _settingService.DeleteSettingAsync<CheckEnginePluginSettings>();
        await _localizationService.DeleteLocaleResourcesAsync("Plugins.TwinParticles.CheckEngine");

        await base.UninstallAsync();
    }
}
