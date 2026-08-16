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
public class AiProviderCompletionContractTests
{
    [Test]
    public async Task OpenAiCompatibleCompletionPort_Should_Satisfy_IAiCompletionPort()
    {
        var options = new CheckEngineAiOptions
        {
            ApiKey = "key",
            BaseUrl = "https://api.openai.com/v1",
            Model = "gpt-4o-mini"
        };

        var port = new OpenAiCompatibleCompletionPort(options, CreateClient("""
            {"choices":[{"message":{"content":"contract-ok"}}],"usage":{"total_tokens":5}}
            """));

        await AssertContract(port, "openai-compatible", "contract-ok");
    }

    [Test]
    public async Task AzureOpenAiCompletionPort_Should_Satisfy_IAiCompletionPort()
    {
        var options = new CheckEngineAiOptions
        {
            ApiKey = "key",
            BaseUrl = "https://example.openai.azure.com",
            AzureDeploymentName = "chat"
        };

        var port = new AzureOpenAiCompletionPort(options, CreateClient("""
            {"choices":[{"message":{"content":"contract-ok"}}],"usage":{"total_tokens":5}}
            """));

        await AssertContract(port, "azure-openai", "contract-ok");
    }

    [Test]
    public async Task AnthropicCompletionPort_Should_Satisfy_IAiCompletionPort()
    {
        var options = new CheckEngineAiOptions
        {
            ApiKey = "key",
            BaseUrl = "https://api.anthropic.com",
            Model = "claude-3-5-haiku-20241022"
        };

        var port = new AnthropicCompletionPort(options, CreateClient("""
            {"content":[{"text":"contract-ok"}],"usage":{"input_tokens":2,"output_tokens":3}}
            """));

        await AssertContract(port, "anthropic", "contract-ok");
    }

    private static async Task AssertContract(IAiCompletionPort port, string providerName, string expectedText)
    {
        var result = await port.CompleteAsync(new AiCompletionRequest
        {
            FeatureKey = AiFeatureKeys.CustomerAssistant,
            PromptKey = AiFeatureKeys.CustomerAssistant,
            Prompt = "What fits my BMW?",
            MaxTokens = 64,
            Temperature = 0
        }, CancellationToken.None);

        result.Success.Should().BeTrue();
        result.ProviderName.Should().Be(providerName);
        result.Text.Should().Be(expectedText);
        result.TokenUsage.Should().BeGreaterThan(0);
        result.PromptHash.Should().NotBeNullOrWhiteSpace();
    }

    private static HttpClient CreateClient(string payload) =>
        new(new StaticHttpHandler(payload));

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
