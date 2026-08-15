using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using TwinParticles.CheckEngine.Application.Ai;
using TwinParticles.CheckEngine.Domain.Ai;
using TwinParticles.CheckEngine.Domain.Search;

namespace TwinParticles.CheckEngine.Application.Search;

public sealed class NaturalLanguageIntentParser
{
    private static readonly Regex YearRegex = new(@"\b(19|20)\d{2}\b", RegexOptions.Compiled);

    private readonly IAiCompletionPort? _aiCompletionPort;
    private readonly AiPromptResolver? _promptResolver;

    public NaturalLanguageIntentParser(
        IAiCompletionPort? aiCompletionPort = null,
        AiPromptResolver? promptResolver = null)
    {
        _aiCompletionPort = aiCompletionPort;
        _promptResolver = promptResolver;
    }

    public async Task<SearchIntent> ParseAsync(string queryText, string locale, CancellationToken cancellationToken)
    {
        var normalized = queryText?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return new SearchIntent { Locale = locale, KeywordFallback = normalized };
        }

        if (_aiCompletionPort is not null)
        {
            var structured = await TryParseWithLlmAsync(normalized, locale, cancellationToken);
            if (structured is not null)
                return structured;
        }

        return BuildHeuristicIntent(normalized, locale);
    }

    private async Task<SearchIntent?> TryParseWithLlmAsync(string text, string locale, CancellationToken cancellationToken)
    {
        try
        {
            var redacted = AiPromptPrivacy.RedactVins(text);
            var prompt = _promptResolver?.Format(AiFeatureKeys.SearchNaturalLanguage, new Dictionary<string, string?>
            {
                ["locale"] = locale,
                ["query"] = redacted
            }) ?? $"""
                Parse this automotive search query into JSON with keys:
                partTerms (string array), make, model, modelYear (number or null), oemNumber, keywordFallback.
                Return only JSON. Locale={locale}. Query: {redacted}
                """;

            var result = await _aiCompletionPort!.CompleteAsync(new AiCompletionRequest
            {
                FeatureKey = AiFeatureKeys.SearchNaturalLanguage,
                PromptKey = AiFeatureKeys.SearchNaturalLanguage,
                Prompt = prompt,
                MaxTokens = 160,
                Temperature = 0
            }, cancellationToken);

            if (!result.Success || string.IsNullOrWhiteSpace(result.Text))
                return null;

            var json = ExtractJson(result.Text);
            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;

            var partTerms = ReadStringArray(root, "partTerms");
            var modelYear = root.TryGetProperty("modelYear", out var yearElement) && yearElement.ValueKind == JsonValueKind.Number
                ? yearElement.GetInt32()
                : (int?)null;

            return new SearchIntent
            {
                PartTerms = partTerms,
                Make = ReadString(root, "make"),
                Model = ReadString(root, "model"),
                ModelYear = modelYear,
                OemNumber = ReadString(root, "oemNumber"),
                KeywordFallback = ReadString(root, "keywordFallback") ?? string.Join(' ', partTerms),
                Locale = locale,
                ParsedFromNaturalLanguage = true
            };
        }
        catch
        {
            return null;
        }
    }

    private static SearchIntent BuildHeuristicIntent(string text, string locale)
    {
        var yearMatch = YearRegex.Match(text);
        int? year = yearMatch.Success && int.TryParse(yearMatch.Value, out var parsedYear) ? parsedYear : null;

        var tokens = text.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var partTerms = tokens
            .Where(token => !YearRegex.IsMatch(token))
            .Where(token => token.Length > 2)
            .Take(6)
            .ToList();

        return new SearchIntent
        {
            PartTerms = partTerms,
            ModelYear = year,
            KeywordFallback = string.Join(' ', partTerms),
            Locale = locale,
            ParsedFromNaturalLanguage = false
        };
    }

    private static string ExtractJson(string text)
    {
        var start = text.IndexOf('{');
        var end = text.LastIndexOf('}');
        if (start >= 0 && end > start)
            return text[start..(end + 1)];

        return text;
    }

    private static string? ReadString(JsonElement root, string propertyName)
    {
        if (!root.TryGetProperty(propertyName, out var element) || element.ValueKind != JsonValueKind.String)
            return null;

        var value = element.GetString();
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static IReadOnlyList<string> ReadStringArray(JsonElement root, string propertyName)
    {
        if (!root.TryGetProperty(propertyName, out var element) || element.ValueKind != JsonValueKind.Array)
            return [];

        return element.EnumerateArray()
            .Where(x => x.ValueKind == JsonValueKind.String)
            .Select(x => x.GetString()?.Trim())
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x!)
            .ToList();
    }
}
