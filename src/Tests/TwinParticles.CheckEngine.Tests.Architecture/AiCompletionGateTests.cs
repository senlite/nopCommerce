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
        var gate = new AiCompletionGateService(new SuccessPort(), new AlwaysOffToggle());

        var result = await gate.CompleteAsync(new AiCompletionRequest
        {
            FeatureKey = AiFeatureKeys.SearchNaturalLanguage,
            Prompt = "test"
        }, CancellationToken.None);

        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be("ai.disabled");
    }

    [Test]
    public async Task CompleteAsync_Should_Block_When_Ceiling_Exceeded()
    {
        var ledger = new TrackingLedger(10);
        var gate = new AiCompletionGateService(
            new SuccessPort(),
            new AlwaysOnToggle(),
            ledger,
            new FixedSpendPolicy(10, disclosureAcknowledged: true));

        var first = await gate.CompleteAsync(Request(), CancellationToken.None);
        var second = await gate.CompleteAsync(Request(), CancellationToken.None);

        first.Success.Should().BeTrue();
        second.Success.Should().BeFalse();
        second.ErrorCode.Should().Be("ai.ceiling_exceeded");
    }

    [Test]
    public async Task CompleteAsync_Should_Record_Tokens_On_Success()
    {
        var ledger = new TrackingLedger(10_000);
        var gate = new AiCompletionGateService(
            new SuccessPort(),
            new AlwaysOnToggle(),
            ledger,
            new FixedSpendPolicy(10_000, disclosureAcknowledged: true));

        await gate.CompleteAsync(Request(), CancellationToken.None);

        (await ledger.GetDailyUsageAsync(AiFeatureKeys.SearchNaturalLanguage, CancellationToken.None))
            .Should().Be(12);
    }

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
        private readonly int _ceiling;

        public FixedSpendPolicy(int ceiling, bool disclosureAcknowledged)
        {
            _ceiling = ceiling;
            DisclosureAcknowledged = disclosureAcknowledged;
        }

        public bool DisclosureAcknowledged { get; }

        public int ResolveDailyCeiling(string featureKey) => _ceiling;
    }

    private sealed class TrackingLedger : IAiUsageLedger
    {
        private readonly int _ceiling;
        private int _usage;

        public TrackingLedger(int ceiling) => _ceiling = ceiling;

        public Task RecordAsync(string featureKey, int tokenUsage, CancellationToken cancellationToken)
        {
            _usage += tokenUsage;
            return Task.CompletedTask;
        }

        public Task<int> GetDailyUsageAsync(string featureKey, CancellationToken cancellationToken) =>
            Task.FromResult(_usage);

        public Task<bool> IsCeilingExceededAsync(string featureKey, int ceiling, CancellationToken cancellationToken) =>
            Task.FromResult(_usage >= ceiling);
    }
}
