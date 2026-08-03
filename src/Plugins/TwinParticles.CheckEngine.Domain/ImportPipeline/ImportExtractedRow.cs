using System.Collections.Generic;

namespace TwinParticles.CheckEngine.Domain.ImportPipeline;

public sealed class ImportExtractedRow
{
    public int RowNumber { get; init; }

    public IReadOnlyDictionary<string, string?> Fields { get; init; } = new Dictionary<string, string?>();

    public decimal? OcrConfidence { get; init; }
}
