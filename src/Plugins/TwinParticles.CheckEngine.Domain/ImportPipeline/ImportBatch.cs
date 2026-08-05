using System;

namespace TwinParticles.CheckEngine.Domain.ImportPipeline;

public sealed class ImportBatch
{
    public int Id { get; set; }

    public string FileName { get; set; } = string.Empty;

    public ImportSourceFormat SourceFormat { get; set; }

    public string Status { get; set; } = "Received";

    public int? UploadedByCustomerId { get; set; }

    public int RowCount { get; set; }

    public string? ErrorSummary { get; set; }

    public DateTime CreatedUtc { get; set; }

    public DateTime UpdatedUtc { get; set; }
}
