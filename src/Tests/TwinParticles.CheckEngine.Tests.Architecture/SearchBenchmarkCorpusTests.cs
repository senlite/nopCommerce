using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using NUnit.Framework;
using TwinParticles.CheckEngine.Domain.Search;

namespace TwinParticles.CheckEngine.Tests.Architecture;

/// <summary>
/// FR-419 / AC-16.2: the search engine maintains a benchmark query set and must meet a precision
/// budget. This runs the real <c>UnifiedSearchService</c> ranking/paging against the reference
/// in-memory catalog. Production-scale precision needs the seeded reference dataset and is tracked
/// separately in the execution plan.
/// </summary>
[TestFixture]
public class SearchBenchmarkCorpusTests
{
    private sealed class BenchmarkCorpus
    {
        public double PrecisionAtKThreshold { get; set; }
        public int K { get; set; }
        public List<BenchmarkQuery> Queries { get; set; } = [];
    }

    private sealed class BenchmarkQuery
    {
        public string Id { get; set; } = string.Empty;
        public string RawText { get; set; } = string.Empty;
        public string Mode { get; set; } = "Keyword";
        public string Locale { get; set; } = "en";
        public List<int> ExpectedProductIds { get; set; } = [];
    }

    private static BenchmarkCorpus LoadCorpus()
    {
        var start = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        for (var dir = start; dir is not null; dir = dir.Parent)
        {
            var candidate = Path.Combine(dir.FullName, "src", "Tests", "corpus", "search", "benchmark-queries.json");
            if (File.Exists(candidate))
            {
                return JsonSerializer.Deserialize<BenchmarkCorpus>(
                    File.ReadAllText(candidate),
                    new JsonSerializerOptions(JsonSerializerDefaults.Web))!;
            }
        }

        throw new FileNotFoundException("Unable to locate search benchmark-queries.json");
    }

    [Test]
    public async Task Benchmark_Should_Meet_Precision_At_K_Budget()
    {
        var corpus = LoadCorpus();
        corpus.Queries.Should().NotBeEmpty();

        var service = SearchTestSupport.BuildSearchService();
        var precisions = new List<double>();

        foreach (var query in corpus.Queries)
        {
            var mode = Enum.Parse<SearchMode>(query.Mode, ignoreCase: true);
            var result = await service.SearchAsync(new SearchQuery
            {
                RawText = query.RawText,
                Mode = mode,
                Locale = query.Locale,
                Page = 1,
                PageSize = corpus.K
            }, CancellationToken.None);

            var retrieved = result.Hits.Take(corpus.K).Select(hit => hit.ProductId).ToList();
            var expected = query.ExpectedProductIds.ToHashSet();

            var precision = retrieved.Count == 0
                ? (expected.Count == 0 ? 1.0 : 0.0)
                : (double)retrieved.Count(expected.Contains) / retrieved.Count;

            precisions.Add(precision);
        }

        var meanPrecision = precisions.Average();
        meanPrecision.Should().BeGreaterThanOrEqualTo(corpus.PrecisionAtKThreshold,
            $"mean precision@{corpus.K} across the benchmark set must meet the budget");
    }
}
