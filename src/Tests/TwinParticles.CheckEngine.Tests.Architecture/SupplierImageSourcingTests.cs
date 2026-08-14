using System.IO;
using FluentAssertions;
using NUnit.Framework;
using TwinParticles.CheckEngine.Application.Images;

namespace TwinParticles.CheckEngine.Tests.Architecture;

[TestFixture]
public class SupplierImageSourcingTests
{
    [Test]
    public void ParseManifestCsv_Should_Skip_Header_And_Map_Sku_Url()
    {
        var csv = """
                  sku,url,seo
                  ABC-1,https://cdn.example/abc1.jpg,abc-one
                  ABC-2,https://cdn.example/abc2.jpg
                  """;

        var items = SupplierImageSourcingService.ParseManifestCsv(csv);

        items.Should().HaveCount(2);
        items[0].Sku.Should().Be("ABC-1");
        items[0].SourceUrl.Should().Be("https://cdn.example/abc1.jpg");
        items[0].SeoName.Should().Be("abc-one");
        items[1].SeoName.Should().Be("ABC-2");
    }

    [Test]
    public void Image_Admin_Should_Expose_Supplier_Sourcing_Endpoints()
    {
        var controller = ReadPluginFile("Controllers", "ImageAdminController.cs");
        controller.Should().Contain("SourceFromManifest");
        controller.Should().Contain("SourceFromTemplate");
        controller.Should().Contain("SupplierImageSourcingService");
    }

    [Test]
    public void Repository_Should_Ship_Supplier_Image_Script()
    {
        var script = LocateRepoFile("CheckEngine", "scripts", "import-supplier-images.sh");
        File.Exists(script).Should().BeTrue();
        File.ReadAllText(script).Should().Contain("SourceFromManifest");
    }

    private static string ReadPluginFile(params string[] relativePath)
    {
        for (var dir = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
             dir is not null;
             dir = dir.Parent)
        {
            var candidate = Path.Combine([dir.FullName, "src", "Plugins", "TwinParticles.CheckEngine", .. relativePath]);
            if (File.Exists(candidate))
                return File.ReadAllText(candidate);
        }

        throw new FileNotFoundException($"Unable to locate {string.Join('/', relativePath)}");
    }

    private static string LocateRepoFile(params string[] relativePath)
    {
        for (var dir = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
             dir is not null;
             dir = dir.Parent)
        {
            var candidate = Path.Combine([dir.FullName, .. relativePath]);
            if (File.Exists(candidate))
                return candidate;
        }

        throw new FileNotFoundException($"Unable to locate {string.Join('/', relativePath)}");
    }
}
