using System.Collections.Generic;

namespace TwinParticles.CheckEngine.Domain.Images;

public sealed class ImageImportResult
{
    public bool Success { get; init; }

    public int? PictureId { get; init; }

    public bool UsedPlaceholder { get; init; }

    public bool Quarantined { get; init; }

    public string? CdnUrl { get; init; }

    /// <summary>Materialized listing, product and zoom URLs keyed by variant.</summary>
    public IReadOnlyDictionary<ImageVariant, string> VariantUrls { get; init; } =
        new Dictionary<ImageVariant, string>();

    public string? ErrorCode { get; init; }
}
