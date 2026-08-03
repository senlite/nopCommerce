using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using NUnit.Framework;
using TwinParticles.CheckEngine.Application.ImportPipeline.Extraction;
using TwinParticles.CheckEngine.Domain.ImportPipeline;

namespace TwinParticles.CheckEngine.Tests.Architecture;

[TestFixture]
public class ImportExtractionServiceTests
{
    [Test]
    public async Task ExtractAsync_Should_Return_Unsupported_When_No_Parser_Can_Handle_Format()
    {
        var service = new ImportExtractionService([new NeverParser()]);

        var result = await service.ExtractAsync(new ImportExtractionRequest
        {
            Format = ImportSourceFormat.Csv,
            FileName = "input.csv",
            Content = []
        }, CancellationToken.None);

        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be("import.unsupported_format");
    }

    [Test]
    public async Task ExtractAsync_Should_Use_Matching_Parser_And_Return_Rows()
    {
        var service = new ImportExtractionService([new CsvFakeParser()]);

        var result = await service.ExtractAsync(new ImportExtractionRequest
        {
            Format = ImportSourceFormat.Csv,
            FileName = "input.csv",
            Content = []
        }, CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Rows.Should().HaveCount(1);
        result.Rows[0].Fields["oem"].Should().Be("11517586925");
    }

    private sealed class NeverParser : IImportExtractionParser
    {
        public bool CanParse(ImportSourceFormat format) => false;

        public Task<IReadOnlyList<ImportExtractedRow>> ParseAsync(ImportExtractionRequest request, CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyList<ImportExtractedRow>>([]);
    }

    private sealed class CsvFakeParser : IImportExtractionParser
    {
        public bool CanParse(ImportSourceFormat format) => format == ImportSourceFormat.Csv;

        public Task<IReadOnlyList<ImportExtractedRow>> ParseAsync(ImportExtractionRequest request, CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyList<ImportExtractedRow>>([
                new ImportExtractedRow
                {
                    RowNumber = 1,
                    Fields = new Dictionary<string, string?>
                    {
                        ["oem"] = "11517586925"
                    }
                }
            ]);
    }
}
