using System.IO;
using FluentAssertions;
using NUnit.Framework;

namespace TwinParticles.CheckEngine.Tests.Architecture;

[TestFixture]
public class SearchEmbeddingSqlContractTests
{
    [Test]
    public void Catalog_Source_Should_Treat_Model_Hash_And_Keyword_Projection_As_Stale()
    {
        var source = ReadInfrastructure("Search", "SqlSearchEmbeddingCatalogSource.cs");

        source.Should().Contain("se.ProductId IS NULL OR si.UpdatedUtc > se.UpdatedUtc");
        source.Should().Contain("se.ModelHash <> @modelHash");
        source.Should().Contain("SearchEmbeddingStaleOptions");
        source.Should().Contain("_synonyms.Expand");
    }

    [Test]
    public void Refresh_Task_Should_Bootstrap_Empty_Index_And_Respect_Feature_Toggle()
    {
        var source = ReadPlugin("Tasks", "SearchEmbeddingRefreshTask.cs");

        source.Should().Contain("AiFeatureKeys.SearchSemantic");
        source.Should().Contain("vectorCount == 0");
        source.Should().Contain("RebuildAsync");
        source.Should().Contain("RefreshIncrementalAsync");
    }

    [Test]
    public void Embedding_Index_Should_Persist_Model_Hash_On_Upsert()
    {
        var source = ReadInfrastructure("Search", "SqlSearchEmbeddingIndex.cs");

        source.Should().Contain("ModelHash = @modelHash");
        source.Should().Contain("SearchSimilarAsync");
    }

    private static string ReadInfrastructure(params string[] relativePath) => ReadFromRoot(
        ["src", "Plugins", "TwinParticles.CheckEngine.Infrastructure", .. relativePath]);

    private static string ReadPlugin(params string[] relativePath) => ReadFromRoot(
        ["src", "Plugins", "TwinParticles.CheckEngine", .. relativePath]);

    private static string ReadFromRoot(string[] pathParts)
    {
        var start = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        for (var dir = start; dir is not null; dir = dir.Parent)
        {
            var candidate = Path.Combine([dir.FullName, .. pathParts]);
            if (File.Exists(candidate))
                return File.ReadAllText(candidate);
        }

        throw new FileNotFoundException($"Unable to locate {string.Join('/', pathParts)}");
    }
}
