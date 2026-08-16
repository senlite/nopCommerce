using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TwinParticles.CheckEngine.Domain.Ai;

namespace TwinParticles.CheckEngine.Infrastructure.Ai;

/// <summary>
/// Decorator that caches successful completion responses keyed by prompt version, model, and inputs.
/// </summary>
public sealed class CachingAiCompletionPort : IAiCompletionPort
{
    private readonly IAiCompletionPort _innerPort;
    private readonly IAiCompletionCache? _cache;
    private readonly CheckEngineAiOptions _options;

    public CachingAiCompletionPort(
        IAiCompletionPort innerPort,
        IAiCompletionCache? cache = null,
        CheckEngineAiOptions? options = null)
    {
        _innerPort = innerPort;
        _cache = cache;
        _options = options ?? CheckEngineAiOptions.Current;
    }

    public async Task<AiCompletionResult> CompleteAsync(AiCompletionRequest request, CancellationToken ct)
    {
        if (_cache is null || request.BypassCache)
            return await _innerPort.CompleteAsync(request, ct);

        var cacheKey = BuildCacheKey(request);
        var cached = await _cache.TryGetAsync(cacheKey, ct);
        if (cached is not null)
            return cached;

        var result = await _innerPort.CompleteAsync(request, ct);
        if (result.Success)
            await _cache.SetAsync(cacheKey, result, ct);

        return result;
    }

    internal static string BuildCacheKey(AiCompletionRequest request)
    {
        var normalizedPrompt = request.Prompt?.Trim().ToLowerInvariant() ?? string.Empty;
        var payload = string.Join('\n',
            request.PromptKey ?? string.Empty,
            request.FeatureKey ?? string.Empty,
            normalizedPrompt,
            request.MaxTokens.ToString(),
            request.Temperature.ToString("F2"),
            CheckEngineAiOptions.Current.Model ?? string.Empty);

        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(payload));
        return Convert.ToHexString(hash);
    }
}
