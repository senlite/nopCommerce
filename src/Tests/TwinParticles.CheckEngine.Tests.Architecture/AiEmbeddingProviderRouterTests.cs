using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using NUnit.Framework;
using TwinParticles.CheckEngine.Domain.Ai;
using TwinParticles.CheckEngine.Infrastructure.Ai;

namespace TwinParticles.CheckEngine.Tests.Architecture;

[TestFixture]
public class AiEmbeddingProviderRouterTests
{
    [Test]
    public async Task Router_Should_Use_Deterministic_When_Anthropic_Selected()
    {
        var options = new CheckEngineAiOptions
        {
            ProviderKind = AiProviderKind.Anthropic,
            ApiKey = "test-key",
            BaseUrl = "https://example.com"
        };

        var router = new AiEmbeddingProviderRouter(options);
        var result = await router.EmbedAsync(new AiEmbeddingRequest { Text = "brake pad" }, CancellationToken.None);

        result.Success.Should().BeTrue();
        result.ProviderName.Should().Be("deterministic");
    }

    [Test]
    public async Task Router_Should_Prefer_Azure_Port_When_Configured()
    {
        var options = new CheckEngineAiOptions
        {
            ProviderKind = AiProviderKind.AzureOpenAi,
            ApiKey = "test-key",
            BaseUrl = "https://example.openai.azure.com",
            AzureEmbeddingDeploymentName = "embeddings"
        };

        var router = new AiEmbeddingProviderRouter(
            options,
            openAiPort: new OpenAiCompatibleEmbeddingPort(options),
            azurePort: new AzureOpenAiEmbeddingPort(options),
            deterministicPort: new DeterministicTextEmbeddingPort());

        var result = await router.EmbedAsync(new AiEmbeddingRequest { Text = "brake pad" }, CancellationToken.None);

        result.Success.Should().BeTrue();
        result.ProviderName.Should().Be("deterministic");
    }
}
