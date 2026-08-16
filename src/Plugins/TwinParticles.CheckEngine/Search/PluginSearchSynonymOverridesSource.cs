using Nop.Services.Configuration;
using TwinParticles.CheckEngine.Application.Search;
using TwinParticles.CheckEngine.Configuration;
using TwinParticles.CheckEngine.Domain.Search;

namespace TwinParticles.CheckEngine.Search;

public sealed class PluginSearchSynonymOverridesSource : ISearchSynonymOverridesSource
{
    private readonly ISettingService _settingService;

    public PluginSearchSynonymOverridesSource(ISettingService settingService)
    {
        _settingService = settingService;
    }

    public IReadOnlyDictionary<string, string> GetOverrides()
    {
        var settings = _settingService.LoadSettingAsync<CheckEnginePluginSettings>().GetAwaiter().GetResult();
        return SearchSynonymOverridesJson.Parse(settings.SearchSynonymOverridesJson);
    }
}
