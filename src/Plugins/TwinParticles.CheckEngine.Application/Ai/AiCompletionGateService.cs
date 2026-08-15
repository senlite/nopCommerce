using System;
using System.Threading;
using System.Threading.Tasks;
using TwinParticles.CheckEngine.Domain.Ai;

namespace TwinParticles.CheckEngine.Application.Ai;

/// <summary>
/// Central gate for every AI completion call: feature toggle, spend ceiling, and usage ledger.
/// </summary>
public sealed class AiCompletionGateService : IAiCompletionPort
{
    private readonly IAiCompletionPort _innerPort;
    private readonly IAiFeatureToggle? _featureToggle;
    private readonly IAiUsageLedger? _usageLedger;
    private readonly IAiSpendPolicy? _spendPolicy;

    public AiCompletionGateService(
        IAiCompletionPort innerPort,
        IAiFeatureToggle? featureToggle = null,
        IAiUsageLedger? usageLedger = null,
        IAiSpendPolicy? spendPolicy = null)
    {
        _innerPort = innerPort;
        _featureToggle = featureToggle;
        _usageLedger = usageLedger;
        _spendPolicy = spendPolicy;
    }

    public async Task<AiCompletionResult> CompleteAsync(AiCompletionRequest request, CancellationToken ct)
    {
        var featureKey = ResolveFeatureKey(request);

        if (_featureToggle is not null && !_featureToggle.IsEnabled(featureKey))
        {
            return DisabledResult(featureKey, "ai.disabled");
        }

        if (_featureToggle is not null
            && _featureToggle.IsEnabled(featureKey)
            && _spendPolicy is not null
            && !_spendPolicy.DisclosureAcknowledged)
        {
            return DisabledResult(featureKey, "ai.disclosure_required");
        }

        if (_usageLedger is not null && _spendPolicy is not null)
        {
            var ceiling = _spendPolicy.ResolveDailyCeiling(featureKey);
            if (ceiling > 0 && await _usageLedger.IsCeilingExceededAsync(featureKey, ceiling, ct))
            {
                return DisabledResult(featureKey, "ai.ceiling_exceeded");
            }
        }

        var result = await _innerPort.CompleteAsync(request, ct);

        if (result.Success && _usageLedger is not null)
        {
            var tokens = result.TokenUsage > 0 ? result.TokenUsage : EstimateTokens(request, result);
            await _usageLedger.RecordAsync(featureKey, tokens, ct);
        }

        return result;
    }

    private static string ResolveFeatureKey(AiCompletionRequest request)
    {
        if (!string.IsNullOrWhiteSpace(request.FeatureKey))
            return request.FeatureKey;

        if (!string.IsNullOrWhiteSpace(request.PromptKey))
            return request.PromptKey;

        return "ai.unknown";
    }

    private static AiCompletionResult DisabledResult(string featureKey, string errorCode)
    {
        return new AiCompletionResult
        {
            Success = false,
            Text = string.Empty,
            ProviderName = "gate",
            PromptHash = featureKey,
            TokenUsage = 0,
            ErrorCode = errorCode
        };
    }

    private static int EstimateTokens(AiCompletionRequest request, AiCompletionResult result)
    {
        var chars = (request.Prompt?.Length ?? 0) + (result.Text?.Length ?? 0);
        return Math.Max(1, chars / 4);
    }
}
