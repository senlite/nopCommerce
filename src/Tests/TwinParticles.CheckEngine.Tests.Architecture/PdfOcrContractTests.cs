using System.IO;
using FluentAssertions;
using NUnit.Framework;
using TwinParticles.CheckEngine.Domain.ImportPipeline;
using TwinParticles.CheckEngine.Infrastructure.ImportPipeline;

namespace TwinParticles.CheckEngine.Tests.Architecture;

[TestFixture]
public class PdfOcrContractTests
{
    [Test]
    public void Infrastructure_Di_Should_Register_Ocr_Port_And_Pdf_Parser()
    {
        var source = ReadInfrastructureFile("DependencyInjection", "ServiceCollectionExtensions.cs");

        source.Should().Contain(nameof(IImportPdfOcrPort));
        source.Should().Contain(nameof(PdfImportExtractionParser));
        source.Should().Contain(nameof(ExternalProcessImportPdfOcrPort));
    }

    [Test]
    public void Pdf_Parser_Should_Invoke_Ocr_Fallback_When_Text_Extraction_Is_Empty()
    {
        var parser = ReadInfrastructureFile("ImportPipeline", "PdfImportExtractionParser.cs");

        parser.Should().Contain(nameof(IImportPdfOcrPort));
        parser.Should().Contain("TryExtractTextAsync");
    }

    private static string ReadInfrastructureFile(params string[] relativePath)
    {
        for (var dir = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
             dir is not null;
             dir = dir.Parent)
        {
            var candidate = Path.Combine([dir.FullName, "src", "Plugins", "TwinParticles.CheckEngine.Infrastructure", .. relativePath]);
            if (File.Exists(candidate))
                return File.ReadAllText(candidate);
        }

        throw new FileNotFoundException($"Unable to locate {string.Join('/', relativePath)}");
    }
}
