using System.Threading;
using System.Threading.Tasks;
using TwinParticles.CheckEngine.Domain.Ai;

namespace TwinParticles.CheckEngine.Infrastructure.Ai;

public sealed class LayeredAiEmbeddingCache : IAiEmbeddingCache
{
    private readonly MemoryAiEmbeddingCache _memoryCache;
    private readonly IAiEmbeddingCache? _distributedCache;

    public LayeredAiEmbeddingCache(
        MemoryAiEmbeddingCache memoryCache,
        IAiEmbeddingCache? distributedCache = null)
    {
        _memoryCache = memoryCache;
        _distributedCache = distributedCache;
    }

    public async Task<AiEmbeddingResult?> TryGetAsync(string cacheKey, CancellationToken cancellationToken)
    {
        var local = await _memoryCache.TryGetAsync(cacheKey, cancellationToken);
        if (local is not null)
            return local;

        if (_distributedCache is null)
            return null;

        var remote = await _distributedCache.TryGetAsync(cacheKey, cancellationToken);
        if (remote is not null)
            await _memoryCache.SetAsync(cacheKey, remote, cancellationToken);

        return remote;
    }

    public async Task SetAsync(string cacheKey, AiEmbeddingResult result, CancellationToken cancellationToken)
    {
        await _memoryCache.SetAsync(cacheKey, result, cancellationToken);
        if (_distributedCache is not null)
            await _distributedCache.SetAsync(cacheKey, result, cancellationToken);
    }
}
