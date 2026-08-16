using Nop.Services.Configuration;
using TwinParticles.CheckEngine.Application.Ai;
using TwinParticles.CheckEngine.Configuration;
using TwinParticles.CheckEngine.Domain.Ai;

namespace TwinParticles.CheckEngine.Ai;

public sealed class PluginAllowedSpecificationKeyOverridesSource : IAllowedSpecificationKeyOverridesSource
{
    private readonly ISettingService _settingService;

    public PluginAllowedSpecificationKeyOverridesSource(ISettingService settingService)
    {
        _settingService = settingService;
    }

    public SpecificationKeyOverrides GetOverrides()
    {
        var settings = _settingService.LoadSettingAsync<CheckEnginePluginSettings>().GetAwaiter().GetResult();
        return SpecificationKeyOverridesJson.Parse(settings.SpecificationKeyOverridesJson);
    }
}
