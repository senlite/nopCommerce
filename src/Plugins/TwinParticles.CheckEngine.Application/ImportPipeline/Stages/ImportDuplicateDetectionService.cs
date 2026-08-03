using System;
using System.Collections.Generic;
using TwinParticles.CheckEngine.Domain.ImportPipeline;

namespace TwinParticles.CheckEngine.Application.ImportPipeline.Stages;

public sealed class ImportDuplicateDetectionService
{
    public HashSet<int> DetectDuplicateRowNumbers(IReadOnlyList<ImportNormalizedRow> rows)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var duplicates = new HashSet<int>();

        foreach (var row in rows)
        {
            var key = $"{row.OemNumberNormalized ?? string.Empty}|{GetField(row.Fields, "name") ?? string.Empty}";
            if (!seen.Add(key))
                duplicates.Add(row.RowNumber);
        }

        return duplicates;
    }

    private static string? GetField(IReadOnlyDictionary<string, string?> fields, string key)
    {
        return fields.TryGetValue(key, out var value) ? value : null;
    }
}
