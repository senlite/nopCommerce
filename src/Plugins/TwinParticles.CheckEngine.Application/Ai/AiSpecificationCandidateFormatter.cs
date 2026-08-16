using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace TwinParticles.CheckEngine.Application.Ai;

/// <summary>
/// Parses AI specification JSON into reviewable short-description text (FR-521).
/// </summary>
public static class AiSpecificationCandidateFormatter
{
    public static bool TryFormat(string rawOutput, out string formatted)
    {
        formatted = string.Empty;
        if (string.IsNullOrWhiteSpace(rawOutput))
            return false;

        var trimmed = rawOutput.Trim();
        if (!trimmed.StartsWith('['))
        {
            formatted = trimmed;
            return true;
        }

        try
        {
            using var document = JsonDocument.Parse(trimmed);
            if (document.RootElement.ValueKind != JsonValueKind.Array)
                return false;

            var lines = new List<string>();
            foreach (var element in document.RootElement.EnumerateArray())
            {
                var key = ReadProperty(element, "key") ?? ReadProperty(element, "name");
                var value = ReadProperty(element, "value");
                if (string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(value))
                    continue;

                lines.Add($"{key.Trim()}: {value.Trim()}");
            }

            if (lines.Count == 0)
                return false;

            formatted = string.Join('\n', lines);
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private static string? ReadProperty(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var value) || value.ValueKind != JsonValueKind.String)
            return null;

        var text = value.GetString();
        return string.IsNullOrWhiteSpace(text) ? null : text;
    }
}
