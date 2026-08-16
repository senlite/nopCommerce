using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Nop.Core.Caching;
using TwinParticles.CheckEngine.Domain.Ai;

namespace TwinParticles.CheckEngine.Infrastructure.Ai;

/// <summary>
/// Web-farm-safe AI embedding cache backed by nopCommerce's static cache manager.
/// </summary>
public sealed class NopStaticCacheAiEmbeddingCache : IAiEmbeddingCache
{
    private const string Prefix = "TwinParticles.CheckEngine.AiEmbedding.";

    private readonly IStaticCacheManager _cacheManager;
    private readonly CheckEngineAiOptions _options;

    public NopStaticCacheAiEmbeddingCache(IStaticCacheManager cacheManager, CheckEngineAiOptions? options = null)
    {
        _cacheManager = cacheManager;
        _options = options ?? CheckEngineAiOptions.Current;
    }

    public async Task<AiEmbeddingResult?> TryGetAsync(string cacheKey, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(cacheKey))
            return null;

        var key = BuildKey(cacheKey);
        var cached = await _cacheManager.GetAsync<string>(key, defaultValue: null!);
        if (string.IsNullOrWhiteSpace(cached))
            return null;

        try
        {
            return JsonSerializer.Deserialize<AiEmbeddingResult>(cached);
        }
        catch (JsonException)
        {
            await _cacheManager.RemoveAsync(key);
            return null;
        }
    }

    public async Task SetAsync(string cacheKey, AiEmbeddingResult result, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(cacheKey) || !result.Success)
            return;

        var key = BuildKey(cacheKey);
        await _cacheManager.SetAsync(key, JsonSerializer.Serialize(result));
    }

    private CacheKey BuildKey(string cacheKey)
    {
        return new CacheKey($"{Prefix}{cacheKey}")
        {
            CacheTime = Math.Max(1, _options.ResponseCacheTtlMinutes)
        };
    }
}
