using System.Collections.Generic;

namespace TwinParticles.CheckEngine.Models;

public sealed class ImageReplaceRequestModel
{
    public int ProductId { get; set; }

    public string SourceUrl { get; set; } = string.Empty;

    public string SeoName { get; set; } = string.Empty;

    public string? AltTextEn { get; set; }

    public string? AltTextAr { get; set; }
}

public sealed class ImageBatchReplaceRequestModel
{
    public List<ImageBatchReplaceItemModel> Items { get; set; } = [];
}

public sealed class ImageBatchReplaceItemModel
{
    public string Sku { get; set; } = string.Empty;

    public string SourceUrl { get; set; } = string.Empty;

    public string? SeoName { get; set; }

    public string? AltTextEn { get; set; }

    public string? AltTextAr { get; set; }
}
