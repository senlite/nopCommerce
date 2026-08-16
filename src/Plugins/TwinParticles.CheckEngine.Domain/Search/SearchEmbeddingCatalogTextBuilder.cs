namespace TwinParticles.CheckEngine.Domain.Search;

public static class SearchEmbeddingCatalogTextBuilder
{
    public static string Build(params string?[] parts)
    {
        return string.Join(' ',
            parts.Where(part => !string.IsNullOrWhiteSpace(part))
                .Select(part => part!.Trim()));
    }
}
