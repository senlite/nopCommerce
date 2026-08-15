using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using NUnit.Framework;
using TwinParticles.CheckEngine.Application.Search;
using TwinParticles.CheckEngine.Domain.Ai;
using TwinParticles.CheckEngine.Domain.Search;
using TwinParticles.CheckEngine.Infrastructure.Ai;
using TwinParticles.CheckEngine.Infrastructure.Search;

namespace TwinParticles.CheckEngine.Tests.Architecture;

/// <summary>
/// AC-092.1: published natural-language benchmark query set must meet the precision budget.
/// </summary>
[TestFixture]
public class NaturalLanguageBenchmarkCorpusTests
{
    private sealed class BenchmarkCorpus
    {
        public double RecallAtKThreshold { get; set; }
        public int K { get; set; }
        public List<BenchmarkQuery> Queries { get; set; } = [];
    }

    private sealed class BenchmarkQuery
    {
        public string Id { get; set; } = string.Empty;
        public string RawText { get; set; } = string.Empty;
        public string Mode { get; set; } = "NaturalLanguage";
        public string Locale { get; set; } = "en";
        public List<int> ExpectedProductIds { get; set; } = [];
    }

    [Test]
    public async Task NaturalLanguage_Benchmark_Should_Meet_Precision_At_K_Budget()
    {
        var corpus = LoadCorpus();
        var service = await SearchTestSupport.BuildSearchServiceWithSemanticAsync();
        var recalls = new List<double>();

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
            var recall = expected.Count == 0
                ? (retrieved.Count == 0 ? 1.0 : 0.0)
                : (retrieved.Any(expected.Contains) ? 1.0 : 0.0);

            recalls.Add(recall);
        }

        recalls.Average().Should().BeGreaterThanOrEqualTo(corpus.RecallAtKThreshold);
    }

    private static BenchmarkCorpus LoadCorpus()
    {
        var start = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        for (var dir = start; dir is not null; dir = dir.Parent)
        {
            var candidate = Path.Combine(dir.FullName, "src", "Tests", "corpus", "search", "nl-benchmark-queries.json");
            if (File.Exists(candidate))
            {
                return JsonSerializer.Deserialize<BenchmarkCorpus>(
                    File.ReadAllText(candidate),
                    new JsonSerializerOptions(JsonSerializerDefaults.Web))!;
            }
        }

        throw new FileNotFoundException("Unable to locate nl-benchmark-queries.json");
    }
}
