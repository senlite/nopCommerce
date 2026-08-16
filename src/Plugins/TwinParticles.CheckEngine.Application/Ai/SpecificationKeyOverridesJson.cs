using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using TwinParticles.CheckEngine.Domain.Ai;

namespace TwinParticles.CheckEngine.Application.Ai;

public static class SpecificationKeyOverridesJson
{
    public static SpecificationKeyOverrides Parse(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return new SpecificationKeyOverrides();

        try
        {
            var payload = JsonSerializer.Deserialize<SpecificationKeyOverridesFile>(
                json,
                new JsonSerializerOptions(JsonSerializerDefaults.Web));

            return new SpecificationKeyOverrides
            {
                Add = Normalize(payload?.Add),
                Remove = Normalize(payload?.Remove)
            };
        }
        catch (JsonException)
        {
            return new SpecificationKeyOverrides();
        }
    }

    public static string Serialize(SpecificationKeyOverrides overrides)
    {
        var payload = new SpecificationKeyOverridesFile
        {
            Add = Normalize(overrides.Add).OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToList(),
            Remove = Normalize(overrides.Remove).OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToList()
        };

        return JsonSerializer.Serialize(payload, new JsonSerializerOptions(JsonSerializerDefaults.Web)
        {
            WriteIndented = true
        });
    }

    private static List<string> Normalize(IEnumerable<string>? keys)
    {
        if (keys is null)
            return [];

        return keys
            .Where(key => !string.IsNullOrWhiteSpace(key))
            .Select(key => key.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private sealed class SpecificationKeyOverridesFile
    {
        public List<string>? Add { get; set; }

        public List<string>? Remove { get; set; }
    }
}
