using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using NUnit.Framework;
using TwinParticles.CheckEngine.Application.Ai;
using TwinParticles.CheckEngine.Domain.Ai;

namespace TwinParticles.CheckEngine.Tests.Architecture;

[TestFixture]
public class AiEmbeddingGateTests
{
    [Test]
    public async Task EmbedAsync_Should_Block_When_Feature_Disabled()
    {
        var gate = CreateGate(new SuccessPort(), new AlwaysOffToggle(), new TrackingLedger(10_000), new FixedSpendPolicy(10_000, disclosureAcknowledged: true));

        var result = await gate.EmbedAsync(new AiEmbeddingRequest
        {
            FeatureKey = AiFeatureKeys.SearchSemantic,
            Text = "brake pad"
        }, CancellationToken.None);

        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be(AiErrorCodes.Disabled);
    }

    [Test]
    public async Task EmbedAsync_Should_Block_When_Ceiling_Exceeded()
    {
        var ledger = new TrackingLedger(10);
        var gate = CreateGate(new SuccessPort(), new AlwaysOnToggle(), ledger, new FixedSpendPolicy(10, disclosureAcknowledged: true));

        var first = await gate.EmbedAsync(Request(), CancellationToken.None);
        var second = await gate.EmbedAsync(Request(), CancellationToken.None);

        first.Success.Should().BeTrue();
        second.Success.Should().BeFalse();
        second.ErrorCode.Should().Be(AiErrorCodes.BudgetExceeded);
    }

    [Test]
    public async Task EmbedAsync_Should_Record_Tokens_On_Success()
    {
        var ledger = new TrackingLedger(10_000);
        var gate = CreateGate(new SuccessPort(), new AlwaysOnToggle(), ledger, new FixedSpendPolicy(10_000, disclosureAcknowledged: true, globalCeiling: 10_000));

        await gate.EmbedAsync(Request(), CancellationToken.None);

        (await ledger.GetDailyUsageAsync(AiFeatureKeys.SearchSemantic, CancellationToken.None))
            .Should().Be(12);
    }

    [Test]
    public async Task EmbedAsync_Should_Record_Failure_When_Gate_Blocks()
    {
        var ledger = new TrackingLedger(10_000);
        var gate = CreateGate(new SuccessPort(), new AlwaysOffToggle(), ledger, new FixedSpendPolicy(10_000, disclosureAcknowledged: true));

        await gate.EmbedAsync(Request(), CancellationToken.None);

        var summary = await ledger.GetUsageSummaryAsync(AiFeatureKeys.SearchSemantic, CancellationToken.None);
        summary.TodayFailures.Should().Be(1);
        summary.TodayAttempts.Should().Be(1);
    }

    private static AiEmbeddingGateService CreateGate(
        IAiEmbeddingPort inner,
        IAiFeatureToggle toggle,
        IAiUsageLedger ledger,
        IAiSpendPolicy policy) =>
        new(
            inner,
            new AiSpendGuardService(toggle, policy, ledger),
            ledger,
            policy);

    private static AiEmbeddingRequest Request()
    {
        return new AiEmbeddingRequest
        {
            FeatureKey = AiFeatureKeys.SearchSemantic,
            Text = "water pump for 2016 320i"
        };
    }

    private sealed class SuccessPort : IAiEmbeddingPort
    {
        public Task<AiEmbeddingResult> EmbedAsync(AiEmbeddingRequest request, CancellationToken cancellationToken)
        {
            return Task.FromResult(new AiEmbeddingResult
            {
                Success = true,
                Vector = [0.1f, 0.2f],
                ProviderName = "test",
                ModelHash = "abc",
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
        public FixedSpendPolicy(int featureCeiling, bool disclosureAcknowledged, int globalCeiling = 0)
        {
            _ceiling = featureCeiling;
            _globalCeiling = globalCeiling;
            DisclosureAcknowledged = disclosureAcknowledged;
        }

        private readonly int _ceiling;
        private readonly int _globalCeiling;

        public bool DisclosureAcknowledged { get; }

        public int GlobalDailyCeiling => _globalCeiling;

        public decimal TokenCostPer1KUsd => 0.002m;

        public int ResolveDailyCeiling(string featureKey) => _ceiling;
    }

    private sealed class TrackingLedger : IAiUsageLedger
    {
        private int _usage;
        private int _attempts;
        private int _failures;

        public TrackingLedger(int ceiling)
        {
            Ceiling = ceiling;
        }

        public int Ceiling { get; }

        public Task RecordAsync(string featureKey, int tokenUsage, CancellationToken cancellationToken) =>
            RecordOutcomeAsync(featureKey, tokenUsage, success: true, estimatedCostUsd: 0m, cancellationToken);

        public Task RecordOutcomeAsync(
            string featureKey,
            int tokenUsage,
            bool success,
            decimal estimatedCostUsd,
            CancellationToken cancellationToken)
        {
            _attempts++;
            if (success)
                _usage += tokenUsage;
            else
                _failures++;

            return Task.CompletedTask;
        }

        public Task<int> GetDailyUsageAsync(string featureKey, CancellationToken cancellationToken) =>
            Task.FromResult(_usage);

        public Task<int> GetGlobalDailyUsageAsync(CancellationToken cancellationToken) =>
            Task.FromResult(_usage);

        public Task<AiUsageSummary> GetUsageSummaryAsync(string featureKey, CancellationToken cancellationToken) =>
            Task.FromResult(new AiUsageSummary
            {
                FeatureKey = featureKey,
                TodayTokens = _usage,
                TodayAttempts = _attempts,
                TodayFailures = _failures
            });

        public Task<bool> IsCeilingExceededAsync(string featureKey, int ceiling, CancellationToken cancellationToken) =>
            Task.FromResult(_usage >= ceiling);
    }
}
