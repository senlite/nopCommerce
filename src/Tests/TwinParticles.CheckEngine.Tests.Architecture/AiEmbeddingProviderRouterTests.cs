using System.Net;
using System.Net.Http;
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
    public async Task Router_Should_Use_Deterministic_When_Explicitly_Selected()
    {
        var options = new CheckEngineAiOptions
        {
            ProviderKind = AiProviderKind.Anthropic,
            EmbeddingProviderKind = AiEmbeddingProviderKind.Deterministic,
            ApiKey = "test-key",
            BaseUrl = "https://example.com"
        };

        var router = new AiEmbeddingProviderRouter(options);
        var result = await router.EmbedAsync(new AiEmbeddingRequest { Text = "brake pad" }, CancellationToken.None);

        result.Success.Should().BeTrue();
        result.ProviderName.Should().Be("deterministic");
    }

    [Test]
    public async Task Router_Should_Use_OpenAi_Embeddings_For_Anthropic_Completions_When_Configured()
    {
        var options = new CheckEngineAiOptions
        {
            ProviderKind = AiProviderKind.Anthropic,
            EmbeddingProviderKind = AiEmbeddingProviderKind.SameAsCompletion,
            EmbeddingBaseUrl = "https://api.openai.com/v1",
            EmbeddingApiKey = "openai-key",
            EmbeddingModel = "text-embedding-3-small"
        };

        var handler = new StaticHttpHandler("""
            {"data":[{"embedding":[0.1,0.2,0.3]}],"usage":{"total_tokens":4}}
            """);
        var openAiPort = new OpenAiCompatibleEmbeddingPort(options, new HttpClient(handler));
        var router = new AiEmbeddingProviderRouter(options, openAiPort: openAiPort);

        var result = await router.EmbedAsync(new AiEmbeddingRequest { Text = "brake pad" }, CancellationToken.None);

        result.Success.Should().BeTrue();
        result.ProviderName.Should().Be("openai-compatible-embeddings");
        result.Vector.Should().HaveCount(3);
    }

    [Test]
    public async Task Router_Should_Prefer_Azure_Port_When_Configured()
    {
        var options = new CheckEngineAiOptions
        {
            ProviderKind = AiProviderKind.AzureOpenAi,
            EmbeddingProviderKind = AiEmbeddingProviderKind.AzureOpenAi,
            ApiKey = "test-key",
            BaseUrl = "https://example.openai.azure.com",
            AzureEmbeddingDeploymentName = "embeddings"
        };

        var handler = new StaticHttpHandler("""
            {"data":[{"embedding":[0.4,0.5]}],"usage":{"total_tokens":2}}
            """);
        var azurePort = new AzureOpenAiEmbeddingPort(options, new HttpClient(handler));
        var router = new AiEmbeddingProviderRouter(
            options,
            openAiPort: new OpenAiCompatibleEmbeddingPort(options),
            azurePort: azurePort,
            deterministicPort: new DeterministicTextEmbeddingPort());

        var result = await router.EmbedAsync(new AiEmbeddingRequest { Text = "brake pad" }, CancellationToken.None);

        result.Success.Should().BeTrue();
        result.ProviderName.Should().Be("azure-openai-embeddings");
    }

    private sealed class StaticHttpHandler : HttpMessageHandler
    {
        private readonly string _payload;

        public StaticHttpHandler(string payload)
        {
            _payload = payload;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(_payload)
            });
    }
}
