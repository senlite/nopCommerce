using System.IO;
using FluentAssertions;
using NUnit.Framework;
using TwinParticles.CheckEngine.Domain.Search;

namespace TwinParticles.CheckEngine.Tests.Architecture;

[TestFixture]
public class SearchIndexProjectionContractTests
{
    [Test]
    public void Index_State_Should_Only_Be_Ready_After_A_Healthy_Populated_Rebuild()
    {
        new SearchIndexState { IsHealthy = true, IndexedCount = 0 }.IsReady.Should().BeFalse();
        new SearchIndexState { IsHealthy = false, LastRebuildUtc = System.DateTime.UtcNow, IndexedCount = 5 }
            .IsReady.Should().BeFalse();
        new SearchIndexState { IsHealthy = true, LastRebuildUtc = System.DateTime.UtcNow, IndexedCount = 5 }
            .IsReady.Should().BeTrue();
    }

    [Test]
    public void Health_Service_Should_Be_Durable_And_Incrementally_Rebuild_The_Projection()
    {
        var source = ReadInfrastructure("Search", "SqlSearchIndexHealthService.cs");

        source.Should().Contain("TP_CE_SearchIndexState");
        source.Should().Contain("TP_CE_SearchIndex");
        source.Should().Contain("INSERT INTO TP_CE_SearchIndex");
        source.Should().NotContain("MERGE TP_CE_SearchIndex",
            "MERGE is SQL Server-only; the projection must use portable delete+insert");
        source.Should().Contain("p.UpdatedOnUtc > COALESCE((SELECT LastCursorUtc",
            "incremental refresh must only re-project products changed since the cursor");
        source.Should().Contain("RefreshIncrementalAsync");
        // Removed/unpublished products must be evicted so they never surface.
        source.Should().Contain("DELETE si FROM TP_CE_SearchIndex si");
    }

    [Test]
    public void Keyword_Search_Should_Prefer_The_Projection_And_Degrade_To_The_Live_Catalog()
    {
        var source = ReadInfrastructure("Search", "SqlProductSearchReadRepository.cs");

        source.Should().Contain("ISearchIndexStateReader");
        source.Should().Contain("IsProjectionReadyAsync");
        source.Should().Contain("FROM TP_CE_SearchIndex");
        source.Should().Contain("ESCAPE '\\'", "LIKE wildcards from user input must be escaped");
        // Falls back to the authoritative catalog when the projection is empty/unavailable.
        source.Should().Contain("return await SearchNopCatalogAsync(query, text, cancellationToken);");
    }

    private static string ReadInfrastructure(params string[] relativePath)
    {
        var start = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        for (var dir = start; dir is not null; dir = dir.Parent)
        {
            var candidate = Path.Combine(
                [dir.FullName, "src", "Plugins", "TwinParticles.CheckEngine.Infrastructure", .. relativePath]);
            if (File.Exists(candidate))
                return File.ReadAllText(candidate);
        }

        throw new FileNotFoundException($"Unable to locate {string.Join('/', relativePath)}");
    }
}
