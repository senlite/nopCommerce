using System;

namespace TwinParticles.CheckEngine.Domain.Images;

public sealed class ProductImageRecord
{
    public int ProductId { get; set; }

    public int PictureId { get; set; }

    public string? SourceUrl { get; set; }

    public bool IsPlaceholder { get; set; }

    public QuarantineStatus QuarantineStatus { get; set; }

    public DateTime CreatedUtc { get; set; }
}
