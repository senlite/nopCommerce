namespace TwinParticles.CheckEngine.Domain.Ai;

public sealed class AiCompletionResult
{
    public bool Success { get; init; }

    public string Text { get; init; } = string.Empty;

    public string ProviderName { get; init; } = string.Empty;

    public string PromptHash { get; init; } = string.Empty;

    public int TokenUsage { get; init; }

    public string? ErrorCode { get; init; }
}
