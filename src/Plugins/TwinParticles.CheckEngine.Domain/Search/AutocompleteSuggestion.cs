namespace TwinParticles.CheckEngine.Domain.Search;

/// <summary>
/// A single typeahead suggestion (FR-415). Kind is "vehicle", "oem" or "product"; the optional ids
/// let the storefront jump straight to the matching context without a second round-trip.
/// </summary>
public sealed class AutocompleteSuggestion
{
    public string Kind { get; init; } = string.Empty;

    public string Label { get; init; } = string.Empty;

    public string Value { get; init; } = string.Empty;

    public int? ProductId { get; init; }

    public int? VehicleConfigurationId { get; init; }
}
