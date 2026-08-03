namespace TwinParticles.CheckEngine.Domain.ImportPipeline;

public sealed class ImportExtractionRequest
{
    public ImportSourceFormat Format { get; init; }

    public string FileName { get; init; } = string.Empty;

    public byte[] Content { get; init; } = [];

    public string? SupplierProfileCode { get; init; }
}
