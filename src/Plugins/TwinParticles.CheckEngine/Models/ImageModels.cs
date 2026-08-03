namespace TwinParticles.CheckEngine.Models;

public sealed class ImageReplaceRequestModel
{
    public int ProductId { get; set; }

    public string SourceUrl { get; set; } = string.Empty;

    public string SeoName { get; set; } = string.Empty;

    public string? AltTextEn { get; set; }

    public string? AltTextAr { get; set; }
}
