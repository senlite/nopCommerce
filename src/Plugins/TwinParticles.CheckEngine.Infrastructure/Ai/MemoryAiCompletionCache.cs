using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using TwinParticles.CheckEngine.Domain.Ai;

namespace TwinParticles.CheckEngine.Infrastructure.Ai;

public sealed class MemoryAiCompletionCache : IAiCompletionCache
{
    private readonly CheckEngineAiOptions _options;
    private readonly ConcurrentDictionary<string, CacheEntry> _entries = new(StringComparer.Ordinal);

    public MemoryAiCompletionCache(CheckEngineAiOptions? options = null)
    {
        _options = options ?? CheckEngineAiOptions.Current;
    }

    public Task<AiCompletionResult?> TryGetAsync(string cacheKey, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(cacheKey))
            return Task.FromResult<AiCompletionResult?>(null);

        if (!_entries.TryGetValue(cacheKey, out var entry))
            return Task.FromResult<AiCompletionResult?>(null);

        if (entry.ExpiresUtc <= DateTimeOffset.UtcNow)
        {
            _entries.TryRemove(cacheKey, out _);
            return Task.FromResult<AiCompletionResult?>(null);
        }

        return Task.FromResult<AiCompletionResult?>(entry.Result);
    }

    public Task SetAsync(string cacheKey, AiCompletionResult result, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(cacheKey) || !result.Success)
            return Task.CompletedTask;

        var ttlMinutes = Math.Max(1, _options.ResponseCacheTtlMinutes);
        _entries[cacheKey] = new CacheEntry(result, DateTimeOffset.UtcNow.AddMinutes(ttlMinutes));
        return Task.CompletedTask;
    }

    private sealed record CacheEntry(AiCompletionResult Result, DateTimeOffset ExpiresUtc);
}
