using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TwinParticles.CheckEngine.Domain.Ai;

namespace TwinParticles.CheckEngine.Infrastructure.Ai;

public sealed class CachingAiEmbeddingPort : IAiEmbeddingPort
{
    private readonly IAiEmbeddingPort _innerPort;
    private readonly IAiEmbeddingCache? _cache;
    private readonly CheckEngineAiOptions _options;

    public CachingAiEmbeddingPort(
        IAiEmbeddingPort innerPort,
        IAiEmbeddingCache? cache = null,
        CheckEngineAiOptions? options = null)
    {
        _innerPort = innerPort;
        _cache = cache;
        _options = options ?? CheckEngineAiOptions.Current;
    }

    public async Task<AiEmbeddingResult> EmbedAsync(AiEmbeddingRequest request, CancellationToken cancellationToken)
    {
        if (_cache is null || request.BypassCache)
            return await _innerPort.EmbedAsync(request, cancellationToken);

        var cacheKey = BuildCacheKey(request);
        var cached = await _cache.TryGetAsync(cacheKey, cancellationToken);
        if (cached is not null)
            return cached;

        var result = await _innerPort.EmbedAsync(request, cancellationToken);
        if (result.Success)
            await _cache.SetAsync(cacheKey, result, cancellationToken);

        return result;
    }

    internal static string BuildCacheKey(AiEmbeddingRequest request)
    {
        var normalizedText = request.Text?.Trim().ToLowerInvariant() ?? string.Empty;
        var payload = string.Join('\n',
            request.FeatureKey ?? string.Empty,
            request.Locale ?? string.Empty,
            normalizedText,
            CheckEngineAiOptions.Current.EmbeddingModel ?? string.Empty,
            CheckEngineAiOptions.Current.ResolveEffectiveEmbeddingProvider().ToString());

        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(payload));
        return Convert.ToHexString(hash);
    }
}
