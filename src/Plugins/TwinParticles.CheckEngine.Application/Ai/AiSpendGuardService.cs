using System.Threading;
using System.Threading.Tasks;
using TwinParticles.CheckEngine.Domain.Ai;

namespace TwinParticles.CheckEngine.Application.Ai;

public sealed class AiSpendGuardService
{
    private readonly IAiFeatureToggle? _featureToggle;
    private readonly IAiSpendPolicy? _spendPolicy;
    private readonly IAiUsageLedger? _usageLedger;
    private readonly AiSpendAlertService? _alertService;

    public AiSpendGuardService(
        IAiFeatureToggle? featureToggle = null,
        IAiSpendPolicy? spendPolicy = null,
        IAiUsageLedger? usageLedger = null,
        AiSpendAlertService? alertService = null)
    {
        _featureToggle = featureToggle;
        _spendPolicy = spendPolicy;
        _usageLedger = usageLedger;
        _alertService = alertService;
    }

    public async Task<AiSpendGuardResult> CheckAsync(string featureKey, CancellationToken cancellationToken)
    {
        if (_featureToggle is not null && !_featureToggle.IsEnabled(featureKey))
            return AiSpendGuardResult.Deny(featureKey, AiErrorCodes.Disabled);

        if (_featureToggle is not null
            && _featureToggle.IsEnabled(featureKey)
            && _spendPolicy is not null
            && !_spendPolicy.DisclosureAcknowledged)
        {
            return AiSpendGuardResult.Deny(featureKey, AiErrorCodes.DisclosureRequired);
        }

        if (_usageLedger is null || _spendPolicy is null)
            return AiSpendGuardResult.Permit(featureKey);

        var globalCeiling = _spendPolicy.GlobalDailyCeiling;
        if (globalCeiling > 0)
        {
            var globalUsage = await _usageLedger.GetGlobalDailyUsageAsync(cancellationToken);
            if (globalUsage >= globalCeiling)
            {
                await RaiseAlertAsync(featureKey, globalUsage, globalCeiling, global: true, cancellationToken);
                return AiSpendGuardResult.Deny(
                    featureKey,
                    AiErrorCodes.GlobalBudgetExceeded,
                    budgetAlertRequired: true,
                    usage: globalUsage,
                    ceiling: globalCeiling);
            }
        }

        var ceiling = _spendPolicy.ResolveDailyCeiling(featureKey);
        if (ceiling > 0)
        {
            var usage = await _usageLedger.GetDailyUsageAsync(featureKey, cancellationToken);
            if (usage >= ceiling)
            {
                await RaiseAlertAsync(featureKey, usage, ceiling, global: false, cancellationToken);
                return AiSpendGuardResult.Deny(
                    featureKey,
                    AiErrorCodes.BudgetExceeded,
                    budgetAlertRequired: true,
                    usage: usage,
                    ceiling: ceiling);
            }
        }

        return AiSpendGuardResult.Permit(featureKey);
    }

    private async Task RaiseAlertAsync(
        string featureKey,
        int usage,
        int ceiling,
        bool global,
        CancellationToken cancellationToken)
    {
        if (_alertService is null)
            return;

        await _alertService.TryAlertBudgetExceededAsync(featureKey, usage, ceiling, global, cancellationToken);
    }
}
