using System;
using System.Collections.Generic;
using System.Linq;

namespace TwinParticles.CheckEngine.Domain.Search;

/// <summary>
/// Controlled EN↔AR automotive synonyms used to expand bilingual semantic queries (FR-416, FR-511).
/// </summary>
public sealed class BilingualSearchSynonymService
{
    private static readonly IReadOnlyDictionary<string, string> ArabicToEnglish =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["مضخة مياه"] = "water pump",
            ["فلتر زيت"] = "oil filter",
            ["فلتر هواء"] = "air filter",
            ["فلتر مقصورة"] = "cabin filter",
            ["خرطوم رديتر"] = "radiator hose",
            ["رديتر"] = "radiator",
            ["فحمات فرامل"] = "brake pad",
            ["شمعة إشعال"] = "spark plug",
            ["سير التوقيت"] = "timing belt",
            ["دينامو"] = "alternator"
        };

    public string Expand(string text, string locale)
    {
        if (string.IsNullOrWhiteSpace(text))
            return text ?? string.Empty;

        var expanded = text;
        foreach (var pair in ArabicToEnglish.OrderByDescending(x => x.Key.Length))
        {
            if (expanded.Contains(pair.Key, StringComparison.OrdinalIgnoreCase))
                expanded = $"{expanded} {pair.Value}";
        }

        return expanded;
    }
}
