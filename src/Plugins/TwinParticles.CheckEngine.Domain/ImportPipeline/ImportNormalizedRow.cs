using System.Collections.Generic;

namespace TwinParticles.CheckEngine.Domain.ImportPipeline;

public sealed class ImportNormalizedRow
{
    public int RowNumber { get; init; }

    public string? OemNumberRaw { get; init; }

    public string? OemNumberNormalized { get; init; }

    public IReadOnlyDictionary<string, string?> Fields { get; init; } = new Dictionary<string, string?>();
}
