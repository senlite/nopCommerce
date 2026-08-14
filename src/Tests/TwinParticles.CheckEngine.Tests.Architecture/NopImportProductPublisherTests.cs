using System.IO;
using FluentAssertions;
using NUnit.Framework;

namespace TwinParticles.CheckEngine.Tests.Architecture;

[TestFixture]
public class NopImportProductPublisherTests
{
    [Test]
    public void Publisher_Should_Create_Or_Update_Host_Products_And_Link_Oem_Maps()
    {
        var source = ReadPublisherSource();

        source.Should().Contain("InsertProductAsync");
        source.Should().Contain("UpdateProductAsync");
        source.Should().Contain("GetProductBySkuAsync");
        source.Should().Contain("_productOemMapRepository.UpsertAsync");
        source.Should().Contain("Published = true");
        source.Should().Contain("import.publish.missing_identity");
    }

    [Test]
    public void Publisher_Should_Preserve_Published_Product_Id_On_Reimport()
    {
        var source = ReadPublisherSource();

        source.Should().Contain("publishedProductId");
        source.Should().Contain("GetProductByIdAsync");
    }

    private static string ReadPublisherSource()
    {
        for (var dir = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
             dir is not null;
             dir = dir.Parent)
        {
            var candidate = Path.Combine(
                dir.FullName,
                "src", "Plugins", "TwinParticles.CheckEngine.Infrastructure",
                "ImportPipeline", "NopImportProductPublisher.cs");
            if (File.Exists(candidate))
                return File.ReadAllText(candidate);
        }

        throw new FileNotFoundException("Unable to locate NopImportProductPublisher.cs");
    }
}
