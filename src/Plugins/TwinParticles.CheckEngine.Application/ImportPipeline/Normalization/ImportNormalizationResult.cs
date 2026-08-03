using System.Collections.Generic;
using TwinParticles.CheckEngine.Domain.ImportPipeline;

namespace TwinParticles.CheckEngine.Application.ImportPipeline.Normalization;

public sealed class ImportNormalizationResult
{
    public int TotalRows { get; init; }

    public int NormalizedRows { get; init; }

    public IReadOnlyList<ImportNormalizedRow> Rows { get; init; } = [];
}
