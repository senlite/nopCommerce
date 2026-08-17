using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace TwinParticles.CheckEngine.Application.Search;

/// <summary>
/// Heuristic automotive part phrases and chassis codes for NL intent parsing (FR-510).
/// </summary>
internal static class NaturalLanguageIntentHeuristics
{
    internal static readonly Regex ChassisCodePattern = new(@"\b[A-Z]\d{2,3}\b", RegexOptions.Compiled);
    private static readonly Regex OemNumberRegex = new(@"\b(?<oem>\d{2}[\s-]?\d{2}[\s-]?\d[\s-]?\d{3}[\s-]?\d{3})\b", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly string[] KnownPartPhrases =
    [
        "oil filter",
        "air filter",
        "cabin filter",
        "cabin air filter",
        "brake pad",
        "brake pads",
        "water pump",
        "fuel pump",
        "spark plug",
        "spark plugs",
        "timing belt",
        "control arm",
        "shock absorber",
        "wheel bearing",
        "oxygen sensor",
        "radiator hose",
        "head gasket",
        "clutch kit",
        "alternator",
        "starter motor",
        "فلتر زيت",
        "فلتر هواء",
        "فلتر مقصورة",
        "فحمات فرامل",
        "مضخة مياه",
        "سير التوقيت",
        "خرطوم رديتر"
    ];

    public static IReadOnlyList<string> ExtractPartPhrases(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return [];

        var phrases = new List<string>();
        var remaining = text;

        foreach (var phrase in KnownPartPhrases.OrderByDescending(p => p.Length))
        {
            if (remaining.Contains(phrase, StringComparison.OrdinalIgnoreCase))
            {
                phrases.Add(phrase);
                remaining = ReplaceIgnoreCase(remaining, phrase, " ");
            }
        }

        return phrases;
    }

    public static string? TryExtractChassisCode(string text)
    {
        var match = ChassisCodePattern.Match(text);
        return match.Success ? match.Value.ToUpperInvariant() : null;
    }

    public static string? TryExtractOemNumber(string text)
    {
        var match = OemNumberRegex.Match(text);
        if (!match.Success)
            return null;

        return match.Groups["oem"].Value
            .Replace(' ', '-')
            .Replace("--", "-", StringComparison.Ordinal);
    }

    private static string ReplaceIgnoreCase(string source, string oldValue, string newValue)
    {
        var index = source.IndexOf(oldValue, StringComparison.OrdinalIgnoreCase);
        if (index < 0)
            return source;

        return source.Remove(index, oldValue.Length).Insert(index, newValue);
    }
}
