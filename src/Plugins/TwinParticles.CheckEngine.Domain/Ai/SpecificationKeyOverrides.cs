namespace TwinParticles.CheckEngine.Domain.Ai;

/// <summary>
/// Operator overrides for the allowed specification key catalog (FR-521).
/// </summary>
public sealed class SpecificationKeyOverrides
{
    public IReadOnlyList<string> Add { get; init; } = [];

    public IReadOnlyList<string> Remove { get; init; } = [];
}
