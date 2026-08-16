using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using NUnit.Framework;
using TwinParticles.CheckEngine.Application.Ai;
using TwinParticles.CheckEngine.Domain.Ai;

namespace TwinParticles.CheckEngine.Tests.Architecture;

[TestFixture]
public class AiCompletionGateTests
{
    [Test]
    public async Task CompleteAsync_Should_Block_When_Feature_Disabled()
    {
        var gate = CreateGate(new SuccessPort(), new AlwaysOffToggle(), new TrackingLedger(10_000), new FixedSpendPolicy(10_000, disclosureAcknowledged: true));

        var result = await gate.CompleteAsync(new AiCompletionRequest
        {
            FeatureKey = AiFeatureKeys.SearchNaturalLanguage,
            Prompt = "test"
        }, CancellationToken.None);

        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be(AiErrorCodes.Disabled);
    }

    [Test]
    public async Task CompleteAsync_Should_Block_When_Ceiling_Exceeded()
    {
        var ledger = new TrackingLedger(10);
        var gate = CreateGate(new SuccessPort(), new AlwaysOnToggle(), ledger, new FixedSpendPolicy(10, disclosureAcknowledged: true));

        var first = await gate.CompleteAsync(Request(), CancellationToken.None);
        var second = await gate.CompleteAsync(Request(), CancellationToken.None);

        first.Success.Should().BeTrue();
        second.Success.Should().BeFalse();
        second.ErrorCode.Should().Be(AiErrorCodes.BudgetExceeded);
    }

    [Test]
    public async Task CompleteAsync_Should_Record_Tokens_And_Cost_On_Success()
    {
        var ledger = new TrackingLedger(10_000);
        var gate = CreateGate(new SuccessPort(), new AlwaysOnToggle(), ledger, new FixedSpendPolicy(10_000, disclosureAcknowledged: true, costPer1K: 1m, globalCeiling: 10_000));

        await gate.CompleteAsync(Request(), CancellationToken.None);

        (await ledger.GetDailyUsageAsync(AiFeatureKeys.SearchNaturalLanguage, CancellationToken.None))
            .Should().Be(12);
        ledger.LastRecordedCost.Should().Be(0.012m);
    }

    private static AiCompletionGateService CreateGate(
        IAiCompletionPort inner,
        IAiFeatureToggle toggle,
        IAiUsageLedger ledger,
        IAiSpendPolicy policy) =>
        new(
            inner,
            new AiSpendGuardService(toggle, policy, ledger),
            ledger,
            policy);

    private static AiCompletionRequest Request()
    {
        return new AiCompletionRequest
        {
            FeatureKey = AiFeatureKeys.SearchNaturalLanguage,
            PromptKey = AiFeatureKeys.SearchNaturalLanguage,
            Prompt = "water pump for 2016 320i"
        };
    }

    private sealed class SuccessPort : IAiCompletionPort
    {
        public Task<AiCompletionResult> CompleteAsync(AiCompletionRequest request, CancellationToken ct)
        {
            return Task.FromResult(new AiCompletionResult
            {
                Success = true,
                Text = "water pump",
                ProviderName = "test",
                PromptHash = "abc",
                TokenUsage = 12
            });
        }
    }

    private sealed class AlwaysOnToggle : IAiFeatureToggle
    {
        public bool IsEnabled(string featureKey) => true;
    }

    private sealed class AlwaysOffToggle : IAiFeatureToggle
    {
        public bool IsEnabled(string featureKey) => false;
    }

    private sealed class FixedSpendPolicy : IAiSpendPolicy
    {
        public FixedSpendPolicy(int featureCeiling, bool disclosureAcknowledged, decimal costPer1K = 0.002m, int globalCeiling = 0)
        {
            _ceiling = featureCeiling;
            _globalCeiling = globalCeiling;
            DisclosureAcknowledged = disclosureAcknowledged;
            TokenCostPer1KUsd = costPer1K;
        }

        private readonly int _ceiling;
        private readonly int _globalCeiling;

        public bool DisclosureAcknowledged { get; }

        public int GlobalDailyCeiling => _globalCeiling;

        public decimal TokenCostPer1KUsd { get; }

        public int ResolveDailyCeiling(string featureKey) => _ceiling;
    }

    private sealed class TrackingLedger : IAiUsageLedger
    {
        private readonly int _ceiling;
        private int _usage;

        public TrackingLedger(int ceiling) => _ceiling = ceiling;

        public decimal LastRecordedCost { get; private set; }

        public Task RecordAsync(string featureKey, int tokenUsage, CancellationToken cancellationToken) =>
            RecordOutcomeAsync(featureKey, tokenUsage, success: true, estimatedCostUsd: 0m, cancellationToken);

        public Task RecordOutcomeAsync(
            string featureKey,
            int tokenUsage,
            bool success,
            decimal estimatedCostUsd,
            CancellationToken cancellationToken)
        {
            if (success)
            {
                _usage += tokenUsage;
                LastRecordedCost += estimatedCostUsd;
            }

            return Task.CompletedTask;
        }

        public Task<AiUsageSummary> GetUsageSummaryAsync(string featureKey, CancellationToken cancellationToken) =>
            Task.FromResult(new AiUsageSummary
            {
                FeatureKey = featureKey,
                TodayTokens = _usage,
                Last7DaysTokens = _usage,
                Last30DaysTokens = _usage,
                TodayEstimatedCostUsd = LastRecordedCost
            });

        public Task<int> GetDailyUsageAsync(string featureKey, CancellationToken cancellationToken) =>
            Task.FromResult(_usage);

        public Task<int> GetGlobalDailyUsageAsync(CancellationToken cancellationToken) =>
            Task.FromResult(_usage);

        public Task<bool> IsCeilingExceededAsync(string featureKey, int ceiling, CancellationToken cancellationToken) =>
            Task.FromResult(_usage >= ceiling);
    }
}
