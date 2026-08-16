namespace TwinParticles.CheckEngine.Domain.Search;

public static class SearchEmbeddingCatalogTextBuilder
{
    private static readonly BilingualSearchSynonymService DefaultSynonyms = new();

    public static string Build(params string?[] parts)
    {
        return string.Join(' ',
            parts.Where(part => !string.IsNullOrWhiteSpace(part))
                .Select(part => part!.Trim()));
    }

    /// <summary>
    /// Builds catalog embedding text with bilingual synonym expansion for index-time semantic search (FR-416).
    /// </summary>
    public static string BuildForEmbedding(string locale, params string?[] parts)
    {
        var baseText = Build(parts);
        if (string.IsNullOrWhiteSpace(baseText))
            return baseText;

        return DefaultSynonyms.Expand(baseText, locale);
    }
}
