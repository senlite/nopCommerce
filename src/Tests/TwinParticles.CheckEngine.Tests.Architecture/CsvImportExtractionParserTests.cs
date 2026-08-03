using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using NUnit.Framework;
using TwinParticles.CheckEngine.Domain.ImportPipeline;
using TwinParticles.CheckEngine.Infrastructure.ImportPipeline;

namespace TwinParticles.CheckEngine.Tests.Architecture;

[TestFixture]
public class CsvImportExtractionParserTests
{
    [Test]
    public async Task ParseAsync_Should_Parse_Utf8Bom_Csv_Rows()
    {
        var parser = new CsvImportExtractionParser();
        var content = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes("oem,name\n11-51-7-586-925,Oil Filter\n")).ToArray();

        var rows = await parser.ParseAsync(new ImportExtractionRequest
        {
            Format = ImportSourceFormat.Csv,
            FileName = "supplier.csv",
            Content = content
        }, CancellationToken.None);

        rows.Should().HaveCount(1);
        rows[0].Fields.Should().BeEquivalentTo(new Dictionary<string, string?>
        {
            ["oem"] = "11-51-7-586-925",
            ["name"] = "Oil Filter"
        });
    }

    [Test]
    public void CanParse_Should_Be_True_Only_For_Csv()
    {
        var parser = new CsvImportExtractionParser();

        parser.CanParse(ImportSourceFormat.Csv).Should().BeTrue();
        parser.CanParse(ImportSourceFormat.Excel).Should().BeFalse();
        parser.CanParse(ImportSourceFormat.Pdf).Should().BeFalse();
    }
}
