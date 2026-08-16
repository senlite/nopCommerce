namespace TwinParticles.CheckEngine.Domain.Ai;

public sealed class AiEmbeddingResult
{
    public bool Success { get; init; }

    public float[] Vector { get; init; } = [];

    public string ProviderName { get; init; } = string.Empty;

    public string ModelHash { get; init; } = string.Empty;

    public int TokenUsage { get; init; }

    public string? ErrorCode { get; init; }
}
