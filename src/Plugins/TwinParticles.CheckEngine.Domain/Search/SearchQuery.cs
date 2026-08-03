namespace TwinParticles.CheckEngine.Domain.Search;

public sealed class SearchQuery
{
    public string RawText { get; init; } = string.Empty;

    public SearchMode Mode { get; init; } = SearchMode.Auto;

    public int? VehicleConfigurationId { get; init; }

    public bool WidenFitment { get; init; }

    public SearchFilters Filters { get; init; } = new();

    public int Page { get; init; } = 1;

    public int PageSize { get; init; } = 24;

    public string Locale { get; init; } = "en";
}
