namespace TwinParticles.CheckEngine.Domain.Ai;

public sealed class AiCompletionRequest
{
    public string FeatureKey { get; init; } = string.Empty;

    public string PromptKey { get; init; } = string.Empty;

    public string Prompt { get; init; } = string.Empty;

    public int MaxTokens { get; init; } = 512;

    public double Temperature { get; init; } = 0.2;
}
