using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TwinParticles.CheckEngine.Domain.ImportPipeline;
using TwinParticles.CheckEngine.Domain.Oem;

namespace TwinParticles.CheckEngine.Application.ImportPipeline.Normalization;

public sealed class ImportNormalizationService
{
    private readonly IOemNormalizationService _oemNormalizationService;

    public ImportNormalizationService(IOemNormalizationService oemNormalizationService)
    {
        _oemNormalizationService = oemNormalizationService;
    }

    public Task<ImportNormalizationResult> NormalizeAsync(IReadOnlyList<ImportExtractedRow> rows, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var normalizedRows = rows.Select(NormalizeRow).ToList();

        var result = new ImportNormalizationResult
        {
            TotalRows = rows.Count,
            NormalizedRows = normalizedRows.Count,
            Rows = normalizedRows
        };

        return Task.FromResult(result);
    }

    private ImportNormalizedRow NormalizeRow(ImportExtractedRow row)
    {
        var normalizedFields = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        foreach (var (key, value) in row.Fields)
        {
            normalizedFields[key] = value?.Trim();
        }

        var rawOem = TryGetFirst(normalizedFields, "oem", "oemnumber", "partnumber");
        var normalizedOem = string.IsNullOrWhiteSpace(rawOem)
            ? null
            : _oemNormalizationService.Normalize(rawOem);

        return new ImportNormalizedRow
        {
            RowNumber = row.RowNumber,
            OemNumberRaw = rawOem,
            OemNumberNormalized = normalizedOem,
            Fields = normalizedFields
        };
    }

    private static string? TryGetFirst(IReadOnlyDictionary<string, string?> fields, params string[] keys)
    {
        foreach (var key in keys)
        {
            if (fields.TryGetValue(key, out var value))
            {
                return value;
            }
        }

        return null;
    }
}
