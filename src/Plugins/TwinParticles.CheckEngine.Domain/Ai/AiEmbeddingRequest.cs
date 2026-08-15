namespace TwinParticles.CheckEngine.Domain.Ai;

public sealed class AiEmbeddingRequest
{
    public string FeatureKey { get; init; } = string.Empty;

    public string Text { get; init; } = string.Empty;

    public string Locale { get; init; } = "en";
}
