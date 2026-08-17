using System;
using System.Collections.Generic;
using System.Linq;
using TwinParticles.CheckEngine.Application.Search;

namespace TwinParticles.CheckEngine.Application.Ai;

/// <summary>
/// Turns a conversational assistant question into catalog retrieval terms.
///
/// The assistant is grounded strictly in retrieved catalog rows, so retrieval quality decides whether it
/// can answer at all. Passing the raw sentence to keyword search matches nothing, because that search
/// looks for the whole phrase, which left the assistant refusing every conversational question.
/// </summary>
public static class AssistantRetrievalQueryBuilder
{
    private const int MaxCandidates = 3;

    private static readonly char[] Separators = [' ', '\t', '\n', '\r', '?', '!', '.', ',', ';', ':', '"', '\'', '(', ')'];

    private static readonly HashSet<string> StopWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "do", "does", "did", "you", "your", "have", "has", "had", "any", "the", "and", "for", "are", "is",
        "was", "can", "could", "would", "should", "will", "get", "got", "need", "want", "looking", "look",
        "please", "what", "which", "where", "there", "here", "stock", "with", "without", "this", "that",
        "these", "those", "buy", "price", "cost", "much", "many", "how", "about", "from", "into", "onto",
        "sell", "sold", "available", "availability", "recommend", "suggest", "some", "one", "also", "just",
        "hello", "hi", "thanks", "thank", "car", "vehicle", "part", "parts", "fit", "fits", "fitment",
        "هل", "لديكم", "عندكم", "متوفر", "متوفرة", "أريد", "اريد", "محتاج", "سعر", "كم", "ما", "هذا",
        "هذه", "على", "من", "في", "الى", "إلى", "مع", "عن", "قطعة", "قطع", "سيارة", "سيارتي"
    };

    /// <summary>
    /// Returns retrieval candidates in decreasing precision: known part phrases first, then the most
    /// specific remaining content words, then the original question as a last resort.
    /// </summary>
    public static IReadOnlyList<string> BuildCandidates(string question)
    {
        var normalized = question?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(normalized))
            return [];

        var candidates = new List<string>();

        foreach (var phrase in NaturalLanguageIntentHeuristics.ExtractPartPhrases(normalized))
            AddCandidate(candidates, phrase);

        var contentWords = normalized
            .Split(Separators, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(word => word.Length > 2 && !StopWords.Contains(word))
            .OrderByDescending(word => word.Length)
            .ToList();

        foreach (var word in contentWords)
            AddCandidate(candidates, word);

        AddCandidate(candidates, normalized);

        return candidates.Take(MaxCandidates).ToList();
    }

    private static void AddCandidate(List<string> candidates, string candidate)
    {
        var trimmed = candidate.Trim();
        if (trimmed.Length == 0)
            return;

        if (!candidates.Contains(trimmed, StringComparer.OrdinalIgnoreCase))
            candidates.Add(trimmed);
    }
}
