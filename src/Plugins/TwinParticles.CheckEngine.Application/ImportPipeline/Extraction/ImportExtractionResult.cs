using System.Collections.Generic;
using TwinParticles.CheckEngine.Domain.ImportPipeline;

namespace TwinParticles.CheckEngine.Application.ImportPipeline.Extraction;

public sealed class ImportExtractionResult
{
    private ImportExtractionResult(bool success, string? errorCode, IReadOnlyList<ImportExtractedRow> rows)
    {
        Success = success;
        ErrorCode = errorCode;
        Rows = rows;
    }

    public bool Success { get; }

    public string? ErrorCode { get; }

    public IReadOnlyList<ImportExtractedRow> Rows { get; }

    public static ImportExtractionResult Ok(IReadOnlyList<ImportExtractedRow> rows) => new(true, null, rows);

    public static ImportExtractionResult Fail(string errorCode) => new(false, errorCode, []);
}
