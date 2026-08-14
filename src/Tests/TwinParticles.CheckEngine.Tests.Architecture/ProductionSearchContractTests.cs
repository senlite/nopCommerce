using System.IO;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using NUnit.Framework;
using TwinParticles.CheckEngine.Infrastructure.Search;

namespace TwinParticles.CheckEngine.Tests.Architecture;

/// <summary>
/// Production search must return real nopCommerce products and fail honestly. The previous
/// repository invented product IDs 1001-1003 whenever SQL was empty or failed, masking catalog and
/// infrastructure defects with plausible-looking results.
/// </summary>
[TestFixture]
public class ProductionSearchContractTests
{
    private static string ReadRepository()
    {
        var start = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        for (var dir = start; dir is not null; dir = dir.Parent)
        {
            var candidate = Path.Combine(
                dir.FullName,
                "src", "Plugins", "TwinParticles.CheckEngine.Infrastructure",
                "Search", "SqlProductSearchReadRepository.cs");
            if (File.Exists(candidate))
                return File.ReadAllText(candidate);
        }

        throw new FileNotFoundException("Unable to locate SqlProductSearchReadRepository.cs");
    }

    [Test]
    public void Production_Repository_Should_Not_Contain_A_Seeded_Fallback()
    {
        var source = ReadRepository();

        source.Should().NotContain("SeededCatalog");
        source.Should().NotContain("BMW Oil Filter");
        source.Should().NotContain("ProductId = 1001");
        source.Should().NotContain("FilterSeeded");
    }

    [Test]
    public void Keyword_And_Category_Search_Should_Use_The_Nop_Catalog_Service()
    {
        var source = ReadRepository();

        source.Should().Contain("IProductService");
        source.Should().Contain("_productService.SearchProductsAsync",
            "nopCommerce applies published/store/locale/category/price visibility consistently");
        source.Should().Contain("visibleIndividuallyOnly: true");
        source.Should().Contain("overridePublished: true");
    }

    [Test]
    public void Oem_And_Vehicle_Projection_Should_Hydrate_Real_Product_Records()
    {
        var source = ReadRepository();

        source.Should().Contain("_productService.GetProductsByIdsAsync");
        source.Should().NotContain("Name = $\"Product {row.ProductId}\"");
    }

    [Test]
    public async Task Search_Health_Should_Report_And_Recover_From_Degradation()
    {
        var health = new InMemorySearchIndexHealthService();

        (await health.IsHealthyAsync(CancellationToken.None)).Should().BeTrue();

        await health.ReportDegradedAsync("catalog failure", CancellationToken.None);
        (await health.IsHealthyAsync(CancellationToken.None)).Should().BeFalse();

        await health.RebuildAsync(CancellationToken.None);
        (await health.IsHealthyAsync(CancellationToken.None)).Should().BeTrue();
    }
}
