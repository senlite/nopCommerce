namespace TwinParticles.CheckEngine.Domain.Ai;

/// <summary>
/// Embedding provider selection. Completions and embeddings may use different vendors
/// (for example Anthropic chat with OpenAI/Azure embeddings).
/// </summary>
public enum AiEmbeddingProviderKind
{
    SameAsCompletion = 0,
    OpenAiCompatible = 1,
    AzureOpenAi = 2,
    Deterministic = 3
}
