using System.Collections.Generic;

namespace TwinParticles.CheckEngine.Domain.Search;

/// <summary>
/// Admin-managed EN↔AR search synonym overrides merged onto the embedded catalog (FR-443).
/// </summary>
public interface ISearchSynonymOverridesSource
{
    IReadOnlyDictionary<string, string> GetOverrides();
}
