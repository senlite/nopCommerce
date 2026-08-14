using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using NUnit.Framework;
using TwinParticles.CheckEngine.Domain.Search;

namespace TwinParticles.CheckEngine.Tests.Architecture;

/// <summary>
/// FR-409 / NFR-001 / AC-16.2: first-page search server latency budget (p95 ≤ 300 ms). This is an
/// in-process microbenchmark over the reference in-memory catalog; it guards the orchestration path
/// (normalize, rank, facet, page). Production-scale latency at the reference dataset is a separate
/// load-test gate noted in the execution plan.
/// </summary>
[TestFixture]
public class SearchFirstPageLatencyBudgetTests
{
    [Test]
    public async Task First_Page_Search_P95_Should_Stay_Within_Budget()
    {
        var service = SearchTestSupport.BuildSearchService();

        SearchQuery Query() => new()
        {
            RawText = "filter",
            Mode = SearchMode.Keyword,
            Locale = "en",
            Page = 1,
            PageSize = 24
        };

        for (var i = 0; i < 20; i++)
            await service.SearchAsync(Query(), CancellationToken.None);

        var samples = new long[200];
        for (var i = 0; i < samples.Length; i++)
        {
            var sw = Stopwatch.StartNew();
            await service.SearchAsync(Query(), CancellationToken.None);
            sw.Stop();
            samples[i] = sw.ElapsedMilliseconds;
        }

        Array.Sort(samples);
        var p95Index = (int)Math.Ceiling(samples.Length * 0.95) - 1;
        var p95 = samples[Math.Clamp(p95Index, 0, samples.Length - 1)];

        p95.Should().BeLessThan(300,
            "NFR-001: first-page search orchestration p95 must stay within the 300ms budget in CI microbench");
    }
}
