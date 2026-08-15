using System;
using System.Threading;
using System.Threading.Tasks;
using TwinParticles.CheckEngine.Domain.Ai;

namespace TwinParticles.CheckEngine.Application.Ai;

/// <summary>
/// Central gate for every AI embedding call: feature toggle, spend ceiling, and usage ledger.
/// </summary>
public sealed class AiEmbeddingGateService : IAiEmbeddingPort
{
    private readonly IAiEmbeddingPort _innerPort;
    private readonly IAiFeatureToggle? _featureToggle;
    private readonly IAiUsageLedger? _usageLedger;
    private readonly IAiSpendPolicy? _spendPolicy;

    public AiEmbeddingGateService(
        IAiEmbeddingPort innerPort,
        IAiFeatureToggle? featureToggle = null,
        IAiUsageLedger? usageLedger = null,
        IAiSpendPolicy? spendPolicy = null)
    {
        _innerPort = innerPort;
        _featureToggle = featureToggle;
        _usageLedger = usageLedger;
        _spendPolicy = spendPolicy;
    }

    public async Task<AiEmbeddingResult> EmbedAsync(AiEmbeddingRequest request, CancellationToken cancellationToken)
    {
        var featureKey = ResolveFeatureKey(request);

        if (_featureToggle is not null && !_featureToggle.IsEnabled(featureKey))
            return await FailAsync(featureKey, "ai.disabled", cancellationToken);

        if (_featureToggle is not null
            && _featureToggle.IsEnabled(featureKey)
            && _spendPolicy is not null
            && !_spendPolicy.DisclosureAcknowledged)
        {
            return await FailAsync(featureKey, "ai.disclosure_required", cancellationToken);
        }

        if (_usageLedger is not null && _spendPolicy is not null)
        {
            var ceiling = _spendPolicy.ResolveDailyCeiling(featureKey);
            if (ceiling > 0 && await _usageLedger.IsCeilingExceededAsync(featureKey, ceiling, cancellationToken))
                return await FailAsync(featureKey, "ai.ceiling_exceeded", cancellationToken);
        }

        var result = await _innerPort.EmbedAsync(request, cancellationToken);

        if (_usageLedger is not null)
        {
            var tokens = result.Success && result.TokenUsage > 0
                ? result.TokenUsage
                : result.Success
                    ? EstimateTokens(request)
                    : 0;
            await _usageLedger.RecordOutcomeAsync(featureKey, tokens, result.Success, cancellationToken);
        }

        return result;
    }

    private async Task<AiEmbeddingResult> FailAsync(string featureKey, string errorCode, CancellationToken cancellationToken)
    {
        if (_usageLedger is not null)
            await _usageLedger.RecordOutcomeAsync(featureKey, 0, success: false, cancellationToken);

        return new AiEmbeddingResult
        {
            Success = false,
            Vector = [],
            ProviderName = "gate",
            ModelHash = featureKey,
            TokenUsage = 0,
            ErrorCode = errorCode
        };
    }

    private static string ResolveFeatureKey(AiEmbeddingRequest request)
    {
        if (!string.IsNullOrWhiteSpace(request.FeatureKey))
            return request.FeatureKey;

        return AiFeatureKeys.SearchSemantic;
    }

    private static int EstimateTokens(AiEmbeddingRequest request)
    {
        return Math.Max(1, (request.Text?.Length ?? 0) / 4);
    }
}
