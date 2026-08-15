namespace TwinParticles.CheckEngine.Domain.Search;

public sealed class SearchEmbeddingDocument
{
    public int ProductId { get; init; }

    public string Locale { get; init; } = "en";

    public string Text { get; init; } = string.Empty;

    public string Name { get; init; } = string.Empty;

    public string? CategoryName { get; init; }

    public string? Brand { get; init; }

    public decimal? Price { get; init; }
}
