namespace TwinParticles.CheckEngine.Domain.L10n;

/// <summary>
/// Operator-editable EN→AR glossary overrides stored in plugin settings (FR-941).
/// </summary>
public interface IAutomotiveGlossaryOverridesSource
{
    IReadOnlyDictionary<string, string> GetOverrides();
}
