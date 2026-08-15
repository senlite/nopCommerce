using FluentAssertions;
using NUnit.Framework;
using TwinParticles.CheckEngine.Domain.Ai;
using TwinParticles.CheckEngine.Infrastructure.Ai;

namespace TwinParticles.CheckEngine.Tests.Architecture;

[TestFixture]
public class CheckEngineAiOptionsExtensionsTests
{
    [Test]
    public void ResolveEffectiveEmbeddingProvider_Should_Default_Anthropic_Completions_To_OpenAi()
    {
        var options = new CheckEngineAiOptions
        {
            ProviderKind = AiProviderKind.Anthropic,
            EmbeddingProviderKind = AiEmbeddingProviderKind.SameAsCompletion
        };

        options.ResolveEffectiveEmbeddingProvider().Should().Be(AiProviderKind.OpenAiCompatible);
    }

    [Test]
    public void ResolveEmbeddingCredentials_Should_Prefer_Dedicated_Embedding_Secrets()
    {
        var options = new CheckEngineAiOptions
        {
            BaseUrl = "https://api.anthropic.com",
            ApiKey = "anthropic-key",
            EmbeddingBaseUrl = "https://api.openai.com/v1",
            EmbeddingApiKey = "openai-key"
        };

        options.ResolveEmbeddingCredentials().Should().Be(("https://api.openai.com/v1", "openai-key"));
    }

    [Test]
    public void HasEmbeddingProviderCredentials_Should_Be_False_For_Deterministic_Provider()
    {
        var options = new CheckEngineAiOptions
        {
            ProviderKind = AiProviderKind.Anthropic,
            EmbeddingProviderKind = AiEmbeddingProviderKind.Deterministic,
            ApiKey = "key",
            BaseUrl = "https://example.com"
        };

        options.HasEmbeddingProviderCredentials().Should().BeFalse();
    }

    [Test]
    public void IsProviderConfigurationValid_Should_Require_Azure_Deployment()
    {
        var options = new CheckEngineAiOptions
        {
            ProviderKind = AiProviderKind.AzureOpenAi,
            ApiKey = "key",
            BaseUrl = "https://example.openai.azure.com",
            AzureDeploymentName = "chat"
        };

        options.IsProviderConfigurationValid().Should().BeTrue();
    }
}
