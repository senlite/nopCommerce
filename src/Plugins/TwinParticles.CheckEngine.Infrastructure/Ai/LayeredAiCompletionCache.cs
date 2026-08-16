using System.Threading;
using System.Threading.Tasks;
using TwinParticles.CheckEngine.Domain.Ai;

namespace TwinParticles.CheckEngine.Infrastructure.Ai;

/// <summary>
/// L1 in-process cache with optional L2 static-cache delegate for web farms.
/// </summary>
public sealed class LayeredAiCompletionCache : IAiCompletionCache
{
    private readonly MemoryAiCompletionCache _memoryCache;
    private readonly IAiCompletionCache? _distributedCache;

    public LayeredAiCompletionCache(
        MemoryAiCompletionCache memoryCache,
        IAiCompletionCache? distributedCache = null)
    {
        _memoryCache = memoryCache;
        _distributedCache = distributedCache;
    }

    public async Task<AiCompletionResult?> TryGetAsync(string cacheKey, CancellationToken cancellationToken)
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

    public async Task SetAsync(string cacheKey, AiCompletionResult result, CancellationToken cancellationToken)
    {
        await _memoryCache.SetAsync(cacheKey, result, cancellationToken);
        if (_distributedCache is not null)
            await _distributedCache.SetAsync(cacheKey, result, cancellationToken);
    }
}
