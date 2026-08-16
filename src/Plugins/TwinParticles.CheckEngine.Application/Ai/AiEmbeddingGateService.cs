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
    private readonly IAiUsageLedger? _usageLedger;
    private readonly IAiSpendPolicy? _spendPolicy;
    private readonly AiSpendGuardService _spendGuard;

    public AiEmbeddingGateService(
        IAiEmbeddingPort innerPort,
        AiSpendGuardService spendGuard,
        IAiUsageLedger? usageLedger = null,
        IAiSpendPolicy? spendPolicy = null)
    {
        _innerPort = innerPort;
        _usageLedger = usageLedger;
        _spendPolicy = spendPolicy;
        _spendGuard = spendGuard;
    }

    public async Task<AiEmbeddingResult> EmbedAsync(AiEmbeddingRequest request, CancellationToken cancellationToken)
    {
        var featureKey = ResolveFeatureKey(request);
        var guard = await _spendGuard.CheckAsync(featureKey, cancellationToken);
        if (!guard.Allowed)
            return await FailAsync(featureKey, guard.ErrorCode ?? AiErrorCodes.Disabled, cancellationToken);

        var result = await _innerPort.EmbedAsync(request, cancellationToken);

        if (_usageLedger is not null)
        {
            var tokens = result.Success && result.TokenUsage > 0
                ? result.TokenUsage
                : result.Success
                    ? EstimateTokens(request)
                    : 0;
            var cost = _spendPolicy is null ? 0m : AiCostEstimator.EstimateUsd(tokens, _spendPolicy.TokenCostPer1KUsd);
            await _usageLedger.RecordOutcomeAsync(featureKey, tokens, result.Success, cost, cancellationToken);
        }

        return result;
    }

    private async Task<AiEmbeddingResult> FailAsync(string featureKey, string errorCode, CancellationToken cancellationToken)
    {
        if (_usageLedger is not null)
            await _usageLedger.RecordOutcomeAsync(featureKey, 0, success: false, estimatedCostUsd: 0m, cancellationToken);

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
