using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using NUnit.Framework;
using TwinParticles.CheckEngine.Domain.Ai;
using TwinParticles.CheckEngine.Infrastructure.Ai;

namespace TwinParticles.CheckEngine.Tests.Architecture;

[TestFixture]
public class CachingAiEmbeddingPortTests
{
    [Test]
    public async Task EmbedAsync_Should_Return_Cached_Result_On_Second_Call()
    {
        var inner = new CountingEmbeddingPort();
        var cache = new MemoryAiEmbeddingCache();
        var port = new CachingAiEmbeddingPort(inner, cache);

        var request = new AiEmbeddingRequest
        {
            FeatureKey = AiFeatureKeys.SearchSemantic,
            Text = "bmw oil filter",
            Locale = "en"
        };

        var first = await port.EmbedAsync(request, CancellationToken.None);
        var second = await port.EmbedAsync(request, CancellationToken.None);

        first.Success.Should().BeTrue();
        second.ModelHash.Should().Be(first.ModelHash);
        inner.CallCount.Should().Be(1);
    }

    private sealed class CountingEmbeddingPort : IAiEmbeddingPort
    {
        public int CallCount { get; private set; }

        public Task<AiEmbeddingResult> EmbedAsync(AiEmbeddingRequest request, CancellationToken cancellationToken)
        {
            CallCount++;
            return Task.FromResult(new AiEmbeddingResult
            {
                Success = true,
                ProviderName = "test",
                ModelHash = "hash",
                Vector = [0.1f, 0.2f],
                TokenUsage = 3
            });
        }
    }
}
