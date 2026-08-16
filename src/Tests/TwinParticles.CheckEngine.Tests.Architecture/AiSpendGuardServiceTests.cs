using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using NUnit.Framework;
using TwinParticles.CheckEngine.Application.Ai;
using TwinParticles.CheckEngine.Domain.Ai;

namespace TwinParticles.CheckEngine.Tests.Architecture;

[TestFixture]
public class AiSpendGuardServiceTests
{
    [Test]
    public async Task CheckAsync_Should_Require_Disclosure_When_Feature_Enabled()
    {
        var guard = new AiSpendGuardService(
            new AlwaysOnToggle(),
            new FixedSpendPolicy(disclosureAcknowledged: false),
            new TrackingLedger());

        var result = await guard.CheckAsync(AiFeatureKeys.CustomerAssistant, CancellationToken.None);

        result.Allowed.Should().BeFalse();
        result.ErrorCode.Should().Be(AiErrorCodes.DisclosureRequired);
    }

    [Test]
    public async Task CheckAsync_Should_Block_Global_Ceiling_Before_Feature_Ceiling()
    {
        var ledger = new TrackingLedger { GlobalUsage = 100 };
        var guard = new AiSpendGuardService(
            new AlwaysOnToggle(),
            new FixedSpendPolicy(globalCeiling: 100, featureCeiling: 500, disclosureAcknowledged: true),
            ledger,
            new AiSpendAlertService());

        var result = await guard.CheckAsync(AiFeatureKeys.SearchSemantic, CancellationToken.None);

        result.Allowed.Should().BeFalse();
        result.ErrorCode.Should().Be(AiErrorCodes.GlobalBudgetExceeded);
    }

    [Test]
    public async Task CheckAsync_Should_Raise_Budget_Alert_Once_Per_Feature_Per_Day()
    {
        var ledger = new TrackingLedger { FeatureUsage = 50 };
        var alerts = new AiSpendAlertService();
        var guard = new AiSpendGuardService(
            new AlwaysOnToggle(),
            new FixedSpendPolicy(globalCeiling: 10_000, featureCeiling: 50, disclosureAcknowledged: true),
            ledger,
            alerts);

        await guard.CheckAsync(AiFeatureKeys.SearchSemantic, CancellationToken.None);
        await guard.CheckAsync(AiFeatureKeys.SearchSemantic, CancellationToken.None);

        alerts.GetRecentAlerts().Should().ContainSingle();
    }

    private sealed class AlwaysOnToggle : IAiFeatureToggle
    {
        public bool IsEnabled(string featureKey) => true;
    }

    private sealed class FixedSpendPolicy : IAiSpendPolicy
    {
        public FixedSpendPolicy(bool disclosureAcknowledged) : this(10_000, 10_000, disclosureAcknowledged)
        {
        }

        public FixedSpendPolicy(int globalCeiling, int featureCeiling, bool disclosureAcknowledged)
        {
            GlobalDailyCeiling = globalCeiling;
            _featureCeiling = featureCeiling;
            DisclosureAcknowledged = disclosureAcknowledged;
        }

        private readonly int _featureCeiling;

        public bool DisclosureAcknowledged { get; }

        public int GlobalDailyCeiling { get; }

        public decimal TokenCostPer1KUsd => 0.002m;

        public int ResolveDailyCeiling(string featureKey) => _featureCeiling;
    }

    private sealed class TrackingLedger : IAiUsageLedger
    {
        public int FeatureUsage { get; init; }

        public int GlobalUsage { get; init; }

        public Task RecordAsync(string featureKey, int tokenUsage, CancellationToken cancellationToken) =>
            Task.CompletedTask;

        public Task RecordOutcomeAsync(
            string featureKey,
            int tokenUsage,
            bool success,
            decimal estimatedCostUsd,
            CancellationToken cancellationToken) =>
            Task.CompletedTask;

        public Task<int> GetDailyUsageAsync(string featureKey, CancellationToken cancellationToken) =>
            Task.FromResult(FeatureUsage);

        public Task<int> GetGlobalDailyUsageAsync(CancellationToken cancellationToken) =>
            Task.FromResult(GlobalUsage);

        public Task<AiUsageSummary> GetUsageSummaryAsync(string featureKey, CancellationToken cancellationToken) =>
            Task.FromResult(new AiUsageSummary { FeatureKey = featureKey });

        public Task<bool> IsCeilingExceededAsync(string featureKey, int ceiling, CancellationToken cancellationToken) =>
            Task.FromResult(FeatureUsage >= ceiling);
    }
}
