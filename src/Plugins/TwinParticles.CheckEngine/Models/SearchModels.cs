using TwinParticles.CheckEngine.Domain.Search;

namespace TwinParticles.CheckEngine.Models;

public sealed class SearchRequestModel
{
    public string RawText { get; set; } = string.Empty;

    public SearchMode Mode { get; set; } = SearchMode.Auto;

    public int? VehicleConfigurationId { get; set; }

    public bool WidenFitment { get; set; }

    public int? CategoryId { get; set; }

    public string? Brand { get; set; }

    public decimal? PriceMin { get; set; }

    public decimal? PriceMax { get; set; }

    public int Page { get; set; } = 1;

    public int PageSize { get; set; } = 24;

    public string Locale { get; set; } = "en";
}
