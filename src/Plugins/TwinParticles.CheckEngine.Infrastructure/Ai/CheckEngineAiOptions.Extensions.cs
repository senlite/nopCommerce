using TwinParticles.CheckEngine.Domain.Ai;

namespace TwinParticles.CheckEngine.Infrastructure.Ai;

public static class CheckEngineAiOptionsExtensions
{
    public static AiProviderKind ResolveEffectiveEmbeddingProvider(this CheckEngineAiOptions options)
    {
        if (options.EmbeddingProviderKind == AiEmbeddingProviderKind.Deterministic)
            return AiProviderKind.Anthropic;

        if (options.EmbeddingProviderKind == AiEmbeddingProviderKind.SameAsCompletion)
        {
            return options.ProviderKind switch
            {
                AiProviderKind.Anthropic => AiProviderKind.OpenAiCompatible,
                _ => options.ProviderKind
            };
        }

        return (AiProviderKind)options.EmbeddingProviderKind;
    }

    public static (string BaseUrl, string ApiKey) ResolveEmbeddingCredentials(this CheckEngineAiOptions options)
    {
        if (!string.IsNullOrWhiteSpace(options.EmbeddingApiKey))
            return (options.EmbeddingBaseUrl.Trim(), options.EmbeddingApiKey.Trim());

        return (options.BaseUrl.Trim(), options.ApiKey.Trim());
    }

    public static bool HasEmbeddingProviderCredentials(this CheckEngineAiOptions options)
    {
        if (options.EmbeddingProviderKind == AiEmbeddingProviderKind.Deterministic)
            return false;

        var provider = options.ResolveEffectiveEmbeddingProvider();
        var (baseUrl, apiKey) = options.ResolveEmbeddingCredentials();

        if (string.IsNullOrWhiteSpace(apiKey) || string.IsNullOrWhiteSpace(baseUrl))
            return false;

        return provider switch
        {
            AiProviderKind.AzureOpenAi => !string.IsNullOrWhiteSpace(options.AzureEmbeddingDeploymentName)
                || !string.IsNullOrWhiteSpace(options.AzureDeploymentName),
            _ => true
        };
    }

    public static bool IsProviderConfigurationValid(this CheckEngineAiOptions options)
    {
        return options.ProviderKind switch
        {
            AiProviderKind.AzureOpenAi => !string.IsNullOrWhiteSpace(options.ApiKey)
                && !string.IsNullOrWhiteSpace(options.BaseUrl)
                && !string.IsNullOrWhiteSpace(options.AzureDeploymentName),
            AiProviderKind.Anthropic => !string.IsNullOrWhiteSpace(options.ApiKey),
            AiProviderKind.OpenAiCompatible => !string.IsNullOrWhiteSpace(options.ApiKey)
                && !string.IsNullOrWhiteSpace(options.BaseUrl),
            _ => false
        };
    }
}
