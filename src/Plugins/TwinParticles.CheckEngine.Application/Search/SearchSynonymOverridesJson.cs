using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace TwinParticles.CheckEngine.Application.Search;

public static class SearchSynonymOverridesJson
{
    public static IReadOnlyDictionary<string, string> Parse(string? json)
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

    public static string Serialize(IReadOnlyDictionary<string, string> terms)
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
