using FluentAssertions;
using NUnit.Framework;

namespace TwinParticles.CheckEngine.Tests.Architecture;

[TestFixture]
public class ImportExtractionConventionsTests
{
    [Test]
    public void ImportSourceFormat_Should_Define_Csv_Excel_And_Pdf()
    {
        var type = typeof(TwinParticles.CheckEngine.Domain.ImportPipeline.ImportSourceFormat);

        System.Enum.IsDefined(type, "Csv").Should().BeTrue();
        System.Enum.IsDefined(type, "Excel").Should().BeTrue();
        System.Enum.IsDefined(type, "Pdf").Should().BeTrue();
    }

    [Test]
    public void Infrastructure_Should_Provide_Csv_Excel_And_Pdf_Parsers()
    {
        typeof(TwinParticles.CheckEngine.Infrastructure.ImportPipeline.CsvImportExtractionParser).IsClass.Should().BeTrue();
        typeof(TwinParticles.CheckEngine.Infrastructure.ImportPipeline.ExcelImportExtractionParser).IsClass.Should().BeTrue();
        typeof(TwinParticles.CheckEngine.Infrastructure.ImportPipeline.PdfImportExtractionParser).IsClass.Should().BeTrue();
    }
}
