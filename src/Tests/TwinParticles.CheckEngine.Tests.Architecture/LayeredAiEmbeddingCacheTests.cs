using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using NUnit.Framework;
using TwinParticles.CheckEngine.Domain.Ai;
using TwinParticles.CheckEngine.Infrastructure.Ai;

namespace TwinParticles.CheckEngine.Tests.Architecture;

[TestFixture]
public class LayeredAiEmbeddingCacheTests
{
    [Test]
    public async Task TryGetAsync_Should_Promote_Distributed_Hit_Into_Memory()
    {
        var memory = new MemoryAiEmbeddingCache();
        var distributed = new RecordingCache();
        var layered = new LayeredAiEmbeddingCache(memory, distributed);

        var cached = new AiEmbeddingResult
        {
            Success = true,
            Vector = [0.1f, 0.2f],
            ProviderName = "test",
            ModelHash = "abc",
            TokenUsage = 4
        };
        await distributed.SetAsync("key", cached, CancellationToken.None);

        var first = await layered.TryGetAsync("key", CancellationToken.None);
        var second = await memory.TryGetAsync("key", CancellationToken.None);

        first.Should().NotBeNull();
        first!.Vector.Should().HaveCount(2);
        second.Should().NotBeNull();
    }

    private sealed class RecordingCache : IAiEmbeddingCache
    {
        private AiEmbeddingResult? _result;

        public Task<AiEmbeddingResult?> TryGetAsync(string cacheKey, CancellationToken cancellationToken) =>
            Task.FromResult(_result);

        public Task SetAsync(string cacheKey, AiEmbeddingResult result, CancellationToken cancellationToken)
        {
            _result = result;
            return Task.CompletedTask;
        }
    }
}
