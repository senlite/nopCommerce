using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using NUnit.Framework;
using TwinParticles.CheckEngine.Domain.Ai;
using TwinParticles.CheckEngine.Infrastructure.Ai;

namespace TwinParticles.CheckEngine.Tests.Architecture;

[TestFixture]
public class InMemoryAiUsageLedgerTests
{
    [Test]
    public async Task RecordAsync_Should_Accumulate_Daily_Usage()
    {
        var ledger = new InMemoryAiUsageLedger();

        await ledger.RecordAsync("search.natural_language", 10, CancellationToken.None);
        await ledger.RecordAsync("search.natural_language", 5, CancellationToken.None);

        (await ledger.GetDailyUsageAsync("search.natural_language", CancellationToken.None)).Should().Be(15);
    }

    [Test]
    public async Task IsCeilingExceededAsync_Should_Be_True_At_Or_Above_Ceiling()
    {
        var ledger = new InMemoryAiUsageLedger();

        await ledger.RecordAsync("import.ai.enrichment", 100, CancellationToken.None);

        (await ledger.IsCeilingExceededAsync("import.ai.enrichment", 100, CancellationToken.None)).Should().BeTrue();
    }

    [Test]
    public async Task GetUsageSummaryAsync_Should_Aggregate_Tokens_And_Failures()
    {
        var ledger = new InMemoryAiUsageLedger();

        await ledger.RecordOutcomeAsync(AiFeatureKeys.SearchSemantic, 40, success: true, estimatedCostUsd: 0.08m, CancellationToken.None);
        await ledger.RecordOutcomeAsync(AiFeatureKeys.SearchSemantic, 0, success: false, estimatedCostUsd: 0m, CancellationToken.None);

        var summary = await ledger.GetUsageSummaryAsync(AiFeatureKeys.SearchSemantic, CancellationToken.None);

        summary.TodayTokens.Should().Be(40);
        summary.Last7DaysTokens.Should().Be(40);
        summary.TodayAttempts.Should().Be(2);
        summary.TodayFailures.Should().Be(1);
        summary.TodayEstimatedCostUsd.Should().Be(0.08m);
        AiUsageSummary.FailureRate(summary.TodayAttempts, summary.TodayFailures).Should().Be(0.5);
    }
}
