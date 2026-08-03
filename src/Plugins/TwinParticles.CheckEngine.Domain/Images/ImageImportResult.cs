namespace TwinParticles.CheckEngine.Domain.Images;

public sealed class ImageImportResult
{
    public bool Success { get; init; }

    public int? PictureId { get; init; }

    public bool UsedPlaceholder { get; init; }

    public bool Quarantined { get; init; }

    public string? CdnUrl { get; init; }

    public string? ErrorCode { get; init; }
}
