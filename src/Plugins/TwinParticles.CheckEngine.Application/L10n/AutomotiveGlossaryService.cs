using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using TwinParticles.CheckEngine.Domain.L10n;

namespace TwinParticles.CheckEngine.Application.L10n;

public sealed class AutomotiveGlossaryService
{
    private static readonly Lazy<IReadOnlyDictionary<string, string>> EmbeddedGlossary = new(LoadEmbeddedGlossary);

    private static readonly IReadOnlyDictionary<string, string> DefaultGlossary = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["water pump"] = "مضخة مياه",
        ["brake pad"] = "فحمات فرامل",
        ["oil filter"] = "فلتر زيت",
        ["air filter"] = "فلتر هواء",
        ["spark plug"] = "شمعة إشعال",
        ["timing belt"] = "سير التوقيت",
        ["alternator"] = "دينامو",
        ["radiator"] = "رديتر",
        ["shock absorber"] = "مساعد",
        ["control arm"] = "مقص",
        ["wheel bearing"] = "رولمان بلي",
        ["fuel pump"] = "طرمبة بنزين",
        ["thermostat"] = "ثرموستات",
        ["clutch"] = "كلتش",
        ["transmission"] = "ناقل حركة",
        ["suspension"] = "تعليق",
        ["exhaust"] = "عادم",
        ["catalytic converter"] = "محول حفاز",
        ["oxygen sensor"] = "حساس أكسجين",
        ["head gasket"] = "جوان رأس"
    };

    private readonly IAutomotiveGlossaryOverridesSource? _overridesSource;

    public AutomotiveGlossaryService(IAutomotiveGlossaryOverridesSource? overridesSource = null)
    {
        _overridesSource = overridesSource;
    }

    public AutomotiveGlossaryService(IReadOnlyDictionary<string, string> englishToArabic)
    {
        _overridesSource = null;
        _fixedTerms = englishToArabic;
    }

    private readonly IReadOnlyDictionary<string, string>? _fixedTerms;

    public IReadOnlyDictionary<string, string> GetTerms()
    {
        if (_fixedTerms is not null)
            return _fixedTerms;

        var merged = new Dictionary<string, string>(EmbeddedGlossary.Value, StringComparer.OrdinalIgnoreCase);
        if (_overridesSource is not null)
        {
            foreach (var pair in _overridesSource.GetOverrides())
                merged[pair.Key] = pair.Value;
        }

        return merged;
    }

    public string BuildGlossaryPromptSection()
    {
        var terms = GetTerms();
        if (terms.Count == 0)
            return string.Empty;

        var lines = terms
            .OrderBy(x => x.Key, StringComparer.OrdinalIgnoreCase)
            .Take(50)
            .Select(pair => $"- {pair.Key} => {pair.Value}");

        return "Use these automotive glossary terms exactly when translating:\n" + string.Join('\n', lines);
    }

    public bool ValidateTranslation(string englishTerm, string translatedText)
    {
        return ScoreTranslation(englishTerm, translatedText) >= 1m;
    }

    /// <summary>
    /// Returns 1 when all glossary terms present in the source appear in the translation;
    /// 0.5 when at least one required term is missing; 1 when no glossary terms apply.
    /// </summary>
    public decimal ScoreTranslation(string englishTerm, string translatedText)
    {
        if (string.IsNullOrWhiteSpace(englishTerm) || string.IsNullOrWhiteSpace(translatedText))
            return 1m;

        var required = 0;
        var matched = 0;
        foreach (var pair in GetTerms())
        {
            if (!englishTerm.Contains(pair.Key, StringComparison.OrdinalIgnoreCase))
                continue;

            required++;
            if (translatedText.Contains(pair.Value, StringComparison.Ordinal))
                matched++;
        }

        if (required == 0)
            return 1m;

        return matched == required ? 1m : 0.5m;
    }

    public Task<string> BuildTranslationPromptAsync(string sourceText, string targetLocale, CancellationToken cancellationToken)
    {
        var glossary = BuildGlossaryPromptSection();
        var prompt = $"Translate the following automotive product text to {targetLocale}. {glossary}\nText: {sourceText}";
        return Task.FromResult(prompt);
    }

    private static IReadOnlyDictionary<string, string> LoadEmbeddedGlossary()
    {
        var assembly = typeof(AutomotiveGlossaryService).Assembly;
        var resourceName = assembly
            .GetManifestResourceNames()
            .FirstOrDefault(name => name.EndsWith("automotive-glossary.json", StringComparison.OrdinalIgnoreCase));

        if (resourceName is null)
            return DefaultGlossary;

        using var stream = assembly.GetManifestResourceStream(resourceName);
        if (stream is null)
            return DefaultGlossary;

        using var reader = new StreamReader(stream);
        var payload = JsonSerializer.Deserialize<GlossaryFile>(
            reader.ReadToEnd(),
            new JsonSerializerOptions(JsonSerializerDefaults.Web));

        if (payload?.Terms is null || payload.Terms.Count == 0)
            return DefaultGlossary;

        return new Dictionary<string, string>(payload.Terms, StringComparer.OrdinalIgnoreCase);
    }

    private sealed class GlossaryFile
    {
        public Dictionary<string, string>? Terms { get; set; }
    }
}
