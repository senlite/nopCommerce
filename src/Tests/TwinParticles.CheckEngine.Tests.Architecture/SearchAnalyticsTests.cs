using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Nop.Core.Domain.Security;
using NUnit.Framework;
using TwinParticles.CheckEngine.Domain.Search;
using TwinParticles.CheckEngine.Infrastructure.Search;

namespace TwinParticles.CheckEngine.Tests.Architecture;

[TestFixture]
public class SearchAnalyticsTests
{
    [Test]
    public void Hmac_Fingerprint_Should_Be_Deterministic_Keyed_And_NonReversible()
    {
        var first = new HmacSearchQueryFingerprintService(new SecuritySettings { EncryptionKey = "key-one-32-characters-long-enough" });
        var second = new HmacSearchQueryFingerprintService(new SecuritySettings { EncryptionKey = "different-key-32-characters-long" });
        const string vin = "wba8e9g50gnu12345";

        var fingerprint = first.Create(vin);

        fingerprint.Should().HaveLength(64);
        fingerprint.Should().NotContain(vin, "raw VIN/query text must never appear in analytics");
        first.Create(vin).Should().Be(fingerprint);
        second.Create(vin).Should().NotBe(fingerprint, "a deployment-specific key prevents portable dictionary reversal");
    }

    [Test]
    public async Task Search_Should_Return_Analytics_Id_Without_Changing_Results()
    {
        var analytics = new RecordingAnalytics();
        var service = SearchTestSupport.BuildSearchService(analyticsService: analytics);

        var result = await service.SearchAsync(new SearchQuery
        {
            RawText = "filter",
            Mode = SearchMode.Keyword,
            Locale = "en"
        }, CancellationToken.None);

        result.Hits.Should().NotBeEmpty();
        result.AnalyticsId.Should().Be(321);
        analytics.LastQuery.Should().Be("filter");
        analytics.LastResultCount.Should().Be(result.Total);
    }

    [Test]
    public async Task Analytics_Failure_Should_Not_Break_Search()
    {
        var service = SearchTestSupport.BuildSearchService(analyticsService: new ThrowingAnalytics());

        var result = await service.SearchAsync(new SearchQuery
        {
            RawText = "filter",
            Mode = SearchMode.Keyword,
            Locale = "en"
        }, CancellationToken.None);

        result.Hits.Should().NotBeEmpty();
        result.AnalyticsId.Should().BeNull();
    }

    [Test]
    public void Sql_Schema_Should_Not_Contain_Raw_Query_Or_Subject_Identifiers()
    {
        var migration = ReadPluginFile(
            "TwinParticles.CheckEngine.Infrastructure", "Migrations", "202608131250_SearchAnalytics.cs");
        var service = ReadPluginFile(
            "TwinParticles.CheckEngine.Infrastructure", "Search", "SqlSearchAnalyticsService.cs");

        migration.Should().Contain("QueryFingerprint");
        migration.Should().NotContain("RawQuery");
        migration.Should().NotContain("CustomerId");
        migration.Should().NotContain("IpAddress");
        migration.Should().NotContain("\"Vin\"");
        migration.Should().NotContain("\"Oem");
        service.Should().Contain("_fingerprintService.Create(normalizedQuery)");
        service.Should().NotContain("new DataParameter(\"raw");
    }

    [Test]
    public void Storefront_Click_Should_Use_Analytics_Id_And_Antiforgery_JsonFetch()
    {
        var route = ReadPluginFile(
            "TwinParticles.CheckEngine", "Infrastructure", "RouteProvider.cs");
        var script = ReadPluginFile(
            "TwinParticles.CheckEngine", "Content", "checkengine-storefront.js");
        var controller = ReadPluginFile(
            "TwinParticles.CheckEngine", "Controllers", "SearchController.cs");

        route.Should().Contain("check-engine/search/click");
        script.Should().Contain("data-ce-search-click");
        script.Should().Contain("analyticsId: analyticsId, productId: productId");
        controller.Should().Contain("[ValidateAntiForgeryToken]");
        controller.Should().Contain("RecordClickAsync");
    }

    private static string ReadPluginFile(string project, params string[] relativePath)
    {
        var start = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        for (var dir = start; dir is not null; dir = dir.Parent)
        {
            var candidate = Path.Combine([dir.FullName, "src", "Plugins", project, .. relativePath]);
            if (File.Exists(candidate))
                return File.ReadAllText(candidate);
        }

        throw new FileNotFoundException($"Unable to locate {project}/{string.Join('/', relativePath)}");
    }

    private class RecordingAnalytics : ISearchAnalyticsService
    {
        public string? LastQuery { get; private set; }
        public int LastResultCount { get; private set; }

        public virtual Task<long?> RecordSearchAsync(
            string normalizedQuery,
            SearchMode mode,
            string locale,
            int resultCount,
            bool hasVehicleContext,
            bool widenFitment,
            bool isDegraded,
            long durationMilliseconds,
            CancellationToken cancellationToken)
        {
            LastQuery = normalizedQuery;
            LastResultCount = resultCount;
            return Task.FromResult<long?>(321);
        }

        public Task RecordClickAsync(long analyticsId, int productId, CancellationToken cancellationToken)
            => Task.CompletedTask;

        public Task<SearchAnalyticsSummary> GetSummaryAsync(DateTime fromUtc, CancellationToken cancellationToken)
            => Task.FromResult(new SearchAnalyticsSummary());

        public Task<int> PruneAsync(DateTime olderThanUtc, CancellationToken cancellationToken)
            => Task.FromResult(0);
    }

    private sealed class ThrowingAnalytics : RecordingAnalytics
    {
        public override Task<long?> RecordSearchAsync(
            string normalizedQuery,
            SearchMode mode,
            string locale,
            int resultCount,
            bool hasVehicleContext,
            bool widenFitment,
            bool isDegraded,
            long durationMilliseconds,
            CancellationToken cancellationToken)
            => throw new InvalidOperationException("analytics unavailable");
    }
}
