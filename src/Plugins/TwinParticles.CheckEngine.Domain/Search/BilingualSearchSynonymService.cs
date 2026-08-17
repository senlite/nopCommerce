using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace TwinParticles.CheckEngine.Domain.Search;

/// <summary>
/// Controlled EN↔AR automotive synonyms used to expand bilingual semantic queries (FR-416, FR-443, FR-511).
/// </summary>
public sealed class BilingualSearchSynonymService
{
    private static readonly Regex ArabicScript = new(@"[\u0600-\u06FF]", RegexOptions.Compiled);
    private static readonly Regex LatinScript = new(@"[A-Za-z]", RegexOptions.Compiled);

    private static readonly IReadOnlyDictionary<string, string> EmbeddedArabicToEnglish =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["مضخة مياه"] = "water pump",
            ["طرمبة بنزين"] = "fuel pump",
            ["فلتر زيت"] = "oil filter",
            ["فلتر هواء"] = "air filter",
            ["فلتر مقصورة"] = "cabin filter",
            ["خرطوم رديتر"] = "radiator hose",
            ["رديتر"] = "radiator",
            ["فحمات فرامل"] = "brake pad",
            ["شمعة إشعال"] = "spark plug",
            ["سير التوقيت"] = "timing belt",
            ["دينامو"] = "alternator",
            ["مساعد"] = "shock absorber",
            ["رولمان"] = "wheel bearing"
        };

    private readonly IReadOnlyDictionary<string, string> _arabicToEnglish;
    private readonly IReadOnlyDictionary<string, string> _englishToArabic;

    public BilingualSearchSynonymService(ISearchSynonymOverridesSource? overridesSource = null)
    {
        _arabicToEnglish = MergeOverrides(overridesSource?.GetOverrides());
        _englishToArabic = _arabicToEnglish
            .GroupBy(pair => pair.Value, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First().Key, StringComparer.OrdinalIgnoreCase);
    }

    public IReadOnlyDictionary<string, string> GetArabicToEnglishPairs() => _arabicToEnglish;

    public string Expand(string text, string locale)
    {
        if (string.IsNullOrWhiteSpace(text))
            return text ?? string.Empty;

        var expanded = text;
        foreach (var pair in _arabicToEnglish.OrderByDescending(x => x.Key.Length))
        {
            if (expanded.Contains(pair.Key, StringComparison.OrdinalIgnoreCase))
                expanded = $"{expanded} {pair.Value}";
        }

        if (ContainsCodeSwitch(text))
        {
            foreach (var pair in _englishToArabic.OrderByDescending(x => x.Key.Length))
            {
                if (expanded.Contains(pair.Key, StringComparison.OrdinalIgnoreCase))
                    expanded = $"{expanded} {pair.Value}";
            }
        }

        return expanded;
    }

    private static IReadOnlyDictionary<string, string> MergeOverrides(IReadOnlyDictionary<string, string>? overrides)
    {
        var merged = new Dictionary<string, string>(EmbeddedArabicToEnglish, StringComparer.OrdinalIgnoreCase);
        if (overrides is null)
            return merged;

        foreach (var pair in overrides)
        {
            if (string.IsNullOrWhiteSpace(pair.Key) || string.IsNullOrWhiteSpace(pair.Value))
                continue;

            merged[pair.Key.Trim()] = pair.Value.Trim();
        }

        return merged;
    }

    private static bool ContainsCodeSwitch(string text)
        => ArabicScript.IsMatch(text) && LatinScript.IsMatch(text);
}
