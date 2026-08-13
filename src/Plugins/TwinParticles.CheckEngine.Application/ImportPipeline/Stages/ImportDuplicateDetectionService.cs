using System;
using System.Collections.Generic;
using TwinParticles.CheckEngine.Domain.ImportPipeline;

namespace TwinParticles.CheckEngine.Application.ImportPipeline.Stages;

public sealed class ImportDuplicateDetectionService
{
    public HashSet<int> DetectDuplicateRowNumbers(IReadOnlyList<ImportNormalizedRow> rows)
        => [.. DetectDuplicateMap(rows).Keys];

    /// <summary>
    /// Maps each duplicate row number to the earlier (original) row it duplicates, so the operator
    /// can make a merge/link/keep-separate decision against a concrete counterpart.
    /// </summary>
    public Dictionary<int, int> DetectDuplicateMap(IReadOnlyList<ImportNormalizedRow> rows)
    {
        var firstByKey = new Dictionary<string, int>(StringComparer.Ordinal);
        var duplicates = new Dictionary<int, int>();

        foreach (var row in rows)
        {
            var key = $"{row.OemNumberNormalized ?? string.Empty}|{GetField(row.Fields, "name") ?? string.Empty}";
            if (firstByKey.TryGetValue(key, out var originalRowNumber))
                duplicates[row.RowNumber] = originalRowNumber;
            else
                firstByKey[key] = row.RowNumber;
        }

        return duplicates;
    }

    private static string? GetField(IReadOnlyDictionary<string, string?> fields, string key)
    {
        return fields.TryGetValue(key, out var value) ? value : null;
    }
}
