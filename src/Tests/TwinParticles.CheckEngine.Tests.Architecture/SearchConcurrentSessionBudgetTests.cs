using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using NUnit.Framework;
using TwinParticles.CheckEngine.Domain.Search;

namespace TwinParticles.CheckEngine.Tests.Architecture;

/// <summary>
/// NFR-017 sample: 2,000 concurrent first-page searches stay inside the NFR-001 p95 budget
/// on the in-process orchestration path. HTTP-level shopper rehearsal is
/// <c>CheckEngine/scripts/run-search-load-gate.sh</c>.
/// </summary>
[TestFixture]
public class SearchConcurrentSessionBudgetTests
{
    [Test]
    public async Task Two_Thousand_Concurrent_First_Page_Searches_Should_Stay_Within_Nfr001_P95()
    {
        var service = SearchTestSupport.BuildSearchService();
        await service.SearchAsync(Query("warmup"), CancellationToken.None);

        const int shoppers = 2000;
        var samples = new long[shoppers];
        var queries = Enumerable.Range(0, shoppers)
            .Select(i => Query(i % 2 == 0 ? "filter" : "oil"))
            .ToArray();

        var wall = Stopwatch.StartNew();
        await Parallel.ForEachAsync(
            Enumerable.Range(0, shoppers),
            new ParallelOptions { MaxDegreeOfParallelism = shoppers },
            async (index, token) =>
            {
                var sw = Stopwatch.StartNew();
                await service.SearchAsync(queries[index], token);
                sw.Stop();
                samples[index] = sw.ElapsedMilliseconds;
            });
        wall.Stop();

        Array.Sort(samples);
        var p95 = samples[(int)Math.Ceiling(samples.Length * 0.95) - 1];
        var p99 = samples[(int)Math.Ceiling(samples.Length * 0.99) - 1];
        TestContext.WriteLine(
            $"NFR-017 in-process {shoppers} concurrent searches: wall={wall.Elapsed.TotalMilliseconds:F1}ms p95={p95}ms p99={p99}ms max={samples[^1]}ms");

        p95.Should().BeLessThanOrEqualTo(300, "NFR-017: 2,000 concurrent first-page searches must hold NFR-001 p95 ≤ 300 ms");
        p99.Should().BeLessThanOrEqualTo(600, "NFR-001 p99 ≤ 600 ms still applies under the concurrent-session sample");
    }

    private static SearchQuery Query(string rawText) => new()
    {
        RawText = rawText,
        Mode = SearchMode.Keyword,
        Locale = "en",
        Page = 1,
        PageSize = 24
    };
}
