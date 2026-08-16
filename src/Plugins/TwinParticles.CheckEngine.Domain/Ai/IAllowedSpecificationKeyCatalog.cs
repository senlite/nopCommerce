namespace TwinParticles.CheckEngine.Domain.Ai;

/// <summary>
/// Canonical specification attribute keys permitted for AI extraction (FR-521).
/// Unknown keys must be flagged for curator review and must not be applied silently.
/// </summary>
public interface IAllowedSpecificationKeyCatalog
{
    IReadOnlyCollection<string> GetAllowedKeys();

    bool IsAllowed(string key);
}
