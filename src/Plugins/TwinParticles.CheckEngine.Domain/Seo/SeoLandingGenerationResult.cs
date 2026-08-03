namespace TwinParticles.CheckEngine.Domain.Seo;

public sealed class SeoLandingGenerationResult
{
    public bool Success { get; init; }

    public SeoLandingPage? Landing { get; init; }

    public string? ErrorCode { get; init; }
}
