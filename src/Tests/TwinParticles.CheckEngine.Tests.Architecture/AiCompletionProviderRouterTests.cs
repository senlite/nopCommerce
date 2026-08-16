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
public class AiCompletionProviderRouterTests
{
    [TestCase(AiProviderKind.OpenAiCompatible, "openai-compatible")]
    [TestCase(AiProviderKind.AzureOpenAi, "azure-openai")]
    [TestCase(AiProviderKind.Anthropic, "anthropic")]
    public async Task Router_Should_Delegate_To_Configured_Provider(AiProviderKind providerKind, string expectedProvider)
    {
        var options = BuildOptions(providerKind);
        var handler = new StubHttpHandler(providerKind);
        var router = new AiCompletionProviderRouter(
            options,
            openAiPort: new OpenAiCompatibleCompletionPort(options, new HttpClient(handler)),
            azurePort: new AzureOpenAiCompletionPort(options, new HttpClient(handler)),
            anthropicPort: new AnthropicCompletionPort(options, new HttpClient(handler)));

        var result = await router.CompleteAsync(new AiCompletionRequest
        {
            FeatureKey = AiFeatureKeys.ImportSeo,
            PromptKey = AiFeatureKeys.ImportSeo,
            Prompt = "hello",
            MaxTokens = 32
        }, CancellationToken.None);

        result.Success.Should().BeTrue();
        result.ProviderName.Should().Be(expectedProvider);
        result.Text.Should().Be("provider-ok");
    }

    private static CheckEngineAiOptions BuildOptions(AiProviderKind providerKind) => new()
    {
        ProviderKind = providerKind,
        ApiKey = "test-key",
        BaseUrl = "https://example.com",
        Model = "gpt-4o-mini",
        AzureDeploymentName = "chat",
        AzureEmbeddingDeploymentName = "embeddings"
    };

    private sealed class StubHttpHandler : HttpMessageHandler
    {
        private readonly AiProviderKind _providerKind;

        public StubHttpHandler(AiProviderKind providerKind)
        {
            _providerKind = providerKind;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var payload = _providerKind switch
            {
                AiProviderKind.Anthropic => """
                    {"content":[{"text":"provider-ok"}],"usage":{"input_tokens":3,"output_tokens":4}}
                    """,
                _ => """
                    {"choices":[{"message":{"content":"provider-ok"}}],"usage":{"total_tokens":7}}
                    """
            };

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(payload)
            });
        }
    }
}
