using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using TwinParticles.CheckEngine.Domain.Ai;

namespace TwinParticles.CheckEngine.Application.Ai;

/// <summary>
/// Parses AI specification JSON into reviewable short-description text (FR-521).
/// </summary>
public static class AiSpecificationCandidateFormatter
{
    public sealed record ValidationResult(
        string FormattedText,
        IReadOnlyList<(string Key, string Value)> Lines,
        IReadOnlyList<string> UnknownKeys,
        decimal QualityScore);

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

    public static ValidationResult Validate(string rawOutput, IAllowedSpecificationKeyCatalog catalog)
    {
        if (!TryFormat(rawOutput, out var formatted))
        {
            return new ValidationResult(
                string.Empty,
                [],
                [],
                0m);
        }

        var lines = ParseLines(formatted);
        var unknownKeys = lines
            .Where(line => !catalog.IsAllowed(line.Key))
            .Select(line => line.Key)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var qualityScore = lines.Count == 0
            ? 0m
            : Math.Round((decimal)(lines.Count - unknownKeys.Count) / lines.Count, 2);

        return new ValidationResult(formatted, lines, unknownKeys, qualityScore);
    }

    public static decimal ComputeQualityScore(IReadOnlyList<(string Key, string Value)> lines, IAllowedSpecificationKeyCatalog catalog)
    {
        if (lines.Count == 0)
            return 0m;

        var unknownCount = lines.Count(line => !catalog.IsAllowed(line.Key));
        return Math.Round((decimal)(lines.Count - unknownCount) / lines.Count, 2);
    }

    private static string? ReadProperty(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var value) || value.ValueKind != JsonValueKind.String)
            return null;

        var text = value.GetString();
        return string.IsNullOrWhiteSpace(text) ? null : text;
    }

    public static IReadOnlyList<(string Key, string Value)> ParseLines(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return [];

        return text.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(line => line.Split(':', 2))
            .Where(parts => parts.Length == 2)
            .Select(parts => (parts[0].Trim(), parts[1].Trim()))
            .Where(pair => !string.IsNullOrWhiteSpace(pair.Item1) && !string.IsNullOrWhiteSpace(pair.Item2))
            .ToList();
    }
}
