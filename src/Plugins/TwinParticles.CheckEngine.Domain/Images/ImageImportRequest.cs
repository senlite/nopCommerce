namespace TwinParticles.CheckEngine.Domain.Images;

public sealed class ImageImportRequest
{
    public int ProductId { get; init; }

    public string? SourceUrl { get; init; }

    public string FallbackSeoName { get; init; } = string.Empty;

    public string? AltTextEn { get; init; }

    public string? AltTextAr { get; init; }
}
