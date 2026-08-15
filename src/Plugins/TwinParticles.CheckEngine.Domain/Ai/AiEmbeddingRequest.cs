namespace TwinParticles.CheckEngine.Domain.Ai;

public sealed class AiEmbeddingRequest
{
    public string FeatureKey { get; init; } = string.Empty;

    public string Text { get; init; } = string.Empty;

    public string Locale { get; init; } = "en";

    /// <summary>When true, bypass the embedding response cache.</summary>
    public bool BypassCache { get; init; }
}
