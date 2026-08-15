using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using NUnit.Framework;
using TwinParticles.CheckEngine.Domain.Ai;
using TwinParticles.CheckEngine.Infrastructure.Ai;

namespace TwinParticles.CheckEngine.Tests.Architecture;

[TestFixture]
public class CachingAiCompletionPortTests
{
    [Test]
    public async Task CompleteAsync_Should_Return_Cached_Result_On_Second_Call()
    {
        var inner = new CountingCompletionPort();
        var cache = new MemoryAiCompletionCache();
        var port = new CachingAiCompletionPort(inner, cache);

        var request = new AiCompletionRequest
        {
            FeatureKey = AiFeatureKeys.ImportSeo,
            PromptKey = AiFeatureKeys.ImportSeo,
            Prompt = "same prompt",
            MaxTokens = 64
        };

        var first = await port.CompleteAsync(request, CancellationToken.None);
        var second = await port.CompleteAsync(request, CancellationToken.None);

        first.Success.Should().BeTrue();
        second.Text.Should().Be(first.Text);
        inner.CallCount.Should().Be(1);
    }

    [Test]
    public async Task CompleteAsync_Should_Bypass_Cache_When_Requested()
    {
        var inner = new CountingCompletionPort();
        var cache = new MemoryAiCompletionCache();
        var port = new CachingAiCompletionPort(inner, cache);

        var request = new AiCompletionRequest
        {
            FeatureKey = AiFeatureKeys.ImportSeo,
            PromptKey = AiFeatureKeys.ImportSeo,
            Prompt = "same prompt",
            BypassCache = true
        };

        await port.CompleteAsync(request, CancellationToken.None);
        await port.CompleteAsync(request, CancellationToken.None);

        inner.CallCount.Should().Be(2);
    }

    private sealed class CountingCompletionPort : IAiCompletionPort
    {
        public int CallCount { get; private set; }

        public Task<AiCompletionResult> CompleteAsync(AiCompletionRequest request, CancellationToken ct)
        {
            CallCount++;
            return Task.FromResult(new AiCompletionResult
            {
                Success = true,
                Text = "cached-body",
                ProviderName = "test",
                PromptHash = "hash",
                TokenUsage = 12
            });
        }
    }
}
