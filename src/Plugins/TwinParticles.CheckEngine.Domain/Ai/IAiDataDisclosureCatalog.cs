using System.Collections.Generic;

namespace TwinParticles.CheckEngine.Domain.Ai;

public interface IAiDataDisclosureCatalog
{
    IReadOnlyList<AiDataDisclosureItem> GetItemsForFeature(string featureKey);
}

public sealed class AiDataDisclosureItem
{
    public string Category { get; init; } = string.Empty;

    public string Description { get; init; } = string.Empty;

    public bool ContainsPersonalData { get; init; }
}
