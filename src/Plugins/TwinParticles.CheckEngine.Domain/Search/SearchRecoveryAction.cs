namespace TwinParticles.CheckEngine.Domain.Search;

/// <summary>
/// A concrete next step offered when a search returns no usable results (FR-412, AC-23.2).
/// The storefront renders these as actionable controls rather than dead-end prose.
/// </summary>
public sealed class SearchRecoveryAction
{
    public string Kind { get; init; } = string.Empty;

    public string Label { get; init; } = string.Empty;

    public string? Query { get; init; }
}
