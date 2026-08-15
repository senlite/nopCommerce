using System.Collections.Generic;

namespace TwinParticles.CheckEngine.Domain.Search;

public sealed class SearchIntent
{
    public IReadOnlyList<string> PartTerms { get; init; } = [];

    public string? Make { get; init; }

    public string? Model { get; init; }

    public int? ModelYear { get; init; }

    public int? VehicleConfigurationId { get; init; }

    public string? OemNumber { get; init; }

    public string Locale { get; init; } = "en";

    public string KeywordFallback { get; init; } = string.Empty;

    public bool ParsedFromNaturalLanguage { get; init; }
}
