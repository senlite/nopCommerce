using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Nop.Services.Configuration;
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
        return ParseOverrides(settings.AutomotiveGlossaryOverridesJson);
    }

    public static IReadOnlyDictionary<string, string> ParseOverrides(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        try
        {
            var terms = JsonSerializer.Deserialize<Dictionary<string, string>>(
                json,
                new JsonSerializerOptions(JsonSerializerDefaults.Web));

            return terms is null || terms.Count == 0
                ? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                : new Dictionary<string, string>(terms, StringComparer.OrdinalIgnoreCase);
        }
        catch (JsonException)
        {
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }
    }

    public static string SerializeOverrides(IReadOnlyDictionary<string, string> terms)
    {
        var ordered = terms
            .OrderBy(pair => pair.Key, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.OrdinalIgnoreCase);

        return JsonSerializer.Serialize(ordered, new JsonSerializerOptions(JsonSerializerDefaults.Web)
        {
            WriteIndented = true
        });
    }
}
