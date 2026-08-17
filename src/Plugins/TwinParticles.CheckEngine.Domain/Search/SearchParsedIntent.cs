namespace TwinParticles.CheckEngine.Domain.Search;

/// <summary>
/// Structured vehicle/part intent produced by natural-language parsing (FR-408 / FR-510).
/// </summary>
public sealed class SearchParsedIntent
{
    public IReadOnlyList<string> PartTerms { get; init; } = [];

    public string? Make { get; init; }

    public string? Model { get; init; }

    public int? ModelYear { get; init; }

    public int? VehicleConfigurationId { get; init; }

    public string? OemNumber { get; init; }

    public bool ParsedFromNaturalLanguage { get; init; }

    public static SearchParsedIntent FromIntent(SearchIntent intent) =>
        new()
        {
            PartTerms = intent.PartTerms,
            Make = intent.Make,
            Model = intent.Model,
            ModelYear = intent.ModelYear,
            VehicleConfigurationId = intent.VehicleConfigurationId,
            OemNumber = intent.OemNumber,
            ParsedFromNaturalLanguage = intent.ParsedFromNaturalLanguage
        };
}
