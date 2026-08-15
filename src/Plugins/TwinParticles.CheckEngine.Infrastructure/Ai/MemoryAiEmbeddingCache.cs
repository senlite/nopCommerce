using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using TwinParticles.CheckEngine.Domain.Ai;

namespace TwinParticles.CheckEngine.Infrastructure.Ai;

public sealed class MemoryAiEmbeddingCache : IAiEmbeddingCache
{
    private readonly CheckEngineAiOptions _options;
    private readonly ConcurrentDictionary<string, CacheEntry> _entries = new(StringComparer.Ordinal);

    public MemoryAiEmbeddingCache(CheckEngineAiOptions? options = null)
    {
        _options = options ?? CheckEngineAiOptions.Current;
    }

    public Task<AiEmbeddingResult?> TryGetAsync(string cacheKey, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(cacheKey))
            return Task.FromResult<AiEmbeddingResult?>(null);

        if (!_entries.TryGetValue(cacheKey, out var entry))
            return Task.FromResult<AiEmbeddingResult?>(null);

        if (entry.ExpiresUtc <= DateTimeOffset.UtcNow)
        {
            _entries.TryRemove(cacheKey, out _);
            return Task.FromResult<AiEmbeddingResult?>(null);
        }

        return Task.FromResult<AiEmbeddingResult?>(entry.Result);
    }

    public Task SetAsync(string cacheKey, AiEmbeddingResult result, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(cacheKey) || !result.Success)
            return Task.CompletedTask;

        var ttlMinutes = Math.Max(1, _options.ResponseCacheTtlMinutes);
        _entries[cacheKey] = new CacheEntry(result, DateTimeOffset.UtcNow.AddMinutes(ttlMinutes));
        return Task.CompletedTask;
    }

    private sealed record CacheEntry(AiEmbeddingResult Result, DateTimeOffset ExpiresUtc);
}
