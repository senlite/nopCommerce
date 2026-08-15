using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using NUnit.Framework;
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
}
