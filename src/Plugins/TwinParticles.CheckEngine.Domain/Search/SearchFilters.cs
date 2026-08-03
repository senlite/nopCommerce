namespace TwinParticles.CheckEngine.Domain.Search;

public sealed class SearchFilters
{
    public int? CategoryId { get; init; }

    public string? Brand { get; init; }

    public decimal? PriceMin { get; init; }

    public decimal? PriceMax { get; init; }
}
