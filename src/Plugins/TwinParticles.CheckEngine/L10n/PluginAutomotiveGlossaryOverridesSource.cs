using System.Collections.Generic;
using Nop.Services.Configuration;
using TwinParticles.CheckEngine.Application.L10n;
using TwinParticles.CheckEngine.Configuration;
using TwinParticles.CheckEngine.Domain.L10n;

namespace TwinParticles.CheckEngine.L10n;

public sealed class PluginAutomotiveGlossaryOverridesSource : IAutomotiveGlossaryOverridesSource
{
    private readonly ISettingService _settingService;

    public PluginAutomotiveGlossaryOverridesSource(ISettingService settingService)
    {
        _settingService = settingService;
    }

    public IReadOnlyDictionary<string, string> GetOverrides()
    {
        var settings = _settingService.LoadSettingAsync<CheckEnginePluginSettings>().GetAwaiter().GetResult();
        return AutomotiveGlossaryOverridesJson.Parse(settings.AutomotiveGlossaryOverridesJson);
    }
}
