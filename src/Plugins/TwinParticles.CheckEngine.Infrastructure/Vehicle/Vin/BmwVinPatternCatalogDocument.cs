using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace TwinParticles.CheckEngine.Infrastructure.Vehicle.Vin;

public sealed class BmwVinPatternCatalogDocument
{
    [JsonPropertyName("version")]
    public int Version { get; init; }

    [JsonPropertyName("wmis")]
    public IReadOnlyList<BmwVinWmiDocumentEntry> Wmis { get; init; } = [];

    [JsonPropertyName("patterns")]
    public IReadOnlyList<BmwVinPatternDocumentEntry> Patterns { get; init; } = [];
}

public sealed class BmwVinWmiDocumentEntry
{
    [JsonPropertyName("wmi")]
    public string Wmi { get; init; } = string.Empty;

    [JsonPropertyName("manufacturerName")]
    public string ManufacturerName { get; init; } = string.Empty;

    [JsonPropertyName("regionCode")]
    public string? RegionCode { get; init; }
}

public sealed class BmwVinPatternDocumentEntry
{
    [JsonPropertyName("pattern")]
    public string Pattern { get; init; } = string.Empty;

    [JsonPropertyName("modelCode")]
    public string ModelCode { get; init; } = string.Empty;

    [JsonPropertyName("generationCode")]
    public string GenerationCode { get; init; } = string.Empty;

    [JsonPropertyName("engineCode")]
    public string? EngineCode { get; init; }

    [JsonPropertyName("trimSlug")]
    public string? TrimSlug { get; init; }

    [JsonPropertyName("confidence")]
    public decimal Confidence { get; init; }

    [JsonPropertyName("priority")]
    public int Priority { get; init; }

    [JsonPropertyName("provenance")]
    public string Provenance { get; init; } = string.Empty;
}
