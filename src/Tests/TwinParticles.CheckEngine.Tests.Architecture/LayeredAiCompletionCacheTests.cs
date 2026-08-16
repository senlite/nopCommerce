using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using NUnit.Framework;
using TwinParticles.CheckEngine.Domain.Ai;
using TwinParticles.CheckEngine.Infrastructure.Ai;

namespace TwinParticles.CheckEngine.Tests.Architecture;

[TestFixture]
public class LayeredAiCompletionCacheTests
{
    [Test]
    public async Task TryGetAsync_Should_Promote_Distributed_Hit_Into_Memory()
    {
        var memory = new MemoryAiCompletionCache();
        var distributed = new RecordingCache();
        var layered = new LayeredAiCompletionCache(memory, distributed);

        var cached = new AiCompletionResult
        {
            Success = true,
            Text = "layered-hit",
            ProviderName = "test",
            PromptHash = "abc",
            TokenUsage = 4
        };
        await distributed.SetAsync("key", cached, CancellationToken.None);

        var first = await layered.TryGetAsync("key", CancellationToken.None);
        var second = await memory.TryGetAsync("key", CancellationToken.None);

        first.Should().NotBeNull();
        first!.Text.Should().Be("layered-hit");
        second.Should().NotBeNull();
        second!.Text.Should().Be("layered-hit");
    }

    [Test]
    public async Task SetAsync_Should_Write_To_Both_Layers()
    {
        var memory = new MemoryAiCompletionCache();
        var distributed = new RecordingCache();
        var layered = new LayeredAiCompletionCache(memory, distributed);
        var cached = new AiCompletionResult
        {
            Success = true,
            Text = "stored",
            ProviderName = "test",
            PromptHash = "abc",
            TokenUsage = 2
        };

        await layered.SetAsync("key", cached, CancellationToken.None);

        (await memory.TryGetAsync("key", CancellationToken.None)).Should().NotBeNull();
        (await distributed.TryGetAsync("key", CancellationToken.None)).Should().NotBeNull();
    }

    private sealed class RecordingCache : IAiCompletionCache
    {
        private AiCompletionResult? _result;

        public Task<AiCompletionResult?> TryGetAsync(string cacheKey, CancellationToken cancellationToken) =>
            Task.FromResult(_result);

        public Task SetAsync(string cacheKey, AiCompletionResult result, CancellationToken cancellationToken)
        {
            _result = result;
            return Task.CompletedTask;
        }
    }
}
