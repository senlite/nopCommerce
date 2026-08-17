namespace TwinParticles.CheckEngine.Domain.Ai;

/// <summary>
/// Grounded catalog source returned with customer assistant answers (H2.10).
/// </summary>
public sealed class AssistantCitation
{
    public int ProductId { get; init; }

    public string Name { get; init; } = string.Empty;

    public string? Brand { get; init; }

    public decimal? Price { get; init; }

    public string? SeName { get; init; }

    /// <summary>Human-readable line injected into the AI prompt.</summary>
    public string DisplayText { get; init; } = string.Empty;
}
