using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

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

    private readonly IReadOnlyDictionary<string, string> _englishToArabic;

    public AutomotiveGlossaryService()
        : this(EmbeddedGlossary.Value)
    {
    }

    public AutomotiveGlossaryService(IReadOnlyDictionary<string, string> englishToArabic)
    {
        _englishToArabic = englishToArabic;
    }

    public string BuildGlossaryPromptSection()
    {
        if (_englishToArabic.Count == 0)
            return string.Empty;

        var lines = _englishToArabic
            .OrderBy(x => x.Key, StringComparer.OrdinalIgnoreCase)
            .Take(50)
            .Select(pair => $"- {pair.Key} => {pair.Value}");

        return "Use these automotive glossary terms exactly when translating:\n" + string.Join('\n', lines);
    }

    public bool ValidateTranslation(string englishTerm, string translatedText)
    {
        if (string.IsNullOrWhiteSpace(englishTerm) || string.IsNullOrWhiteSpace(translatedText))
            return true;

        foreach (var pair in _englishToArabic)
        {
            if (!englishTerm.Contains(pair.Key, StringComparison.OrdinalIgnoreCase))
                continue;

            if (!translatedText.Contains(pair.Value, StringComparison.Ordinal))
                return false;
        }

        return true;
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
