using System.IO;
using FluentAssertions;
using NUnit.Framework;

namespace TwinParticles.CheckEngine.Tests.Architecture;

[TestFixture]
public class ImageDerivativeContractTests
{
    [Test]
    public void Delivery_Service_Should_Materialize_All_Variants_Through_Nop_Media_Pipeline()
    {
        var source = ReadPluginFile(
            "..", "TwinParticles.CheckEngine.Infrastructure", "Images",
            "ConfigurableCdnImageDeliveryService.cs");

        source.Should().Contain("ListingSize = 320");
        source.Should().Contain("ProductSize = 800");
        source.Should().Contain("ZoomSize = 1600");
        source.Should().Contain("_pictureService.GetPictureUrlAsync");
        source.Should().NotContain("$\"/images/{pictureId}",
            "synthetic URLs do not prove derivative storage");
    }

    private static string ReadPluginFile(params string[] relativePath)
    {
        var start = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        for (var dir = start; dir is not null; dir = dir.Parent)
        {
            var candidate = Path.GetFullPath(Path.Combine(
                [dir.FullName, "src", "Plugins", "TwinParticles.CheckEngine", .. relativePath]));
            if (File.Exists(candidate))
                return File.ReadAllText(candidate);
        }

        throw new FileNotFoundException($"Unable to locate {string.Join('/', relativePath)}");
    }
}
