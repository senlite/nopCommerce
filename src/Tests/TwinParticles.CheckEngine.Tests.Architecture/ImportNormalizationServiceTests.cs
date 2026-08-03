using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using NUnit.Framework;
using TwinParticles.CheckEngine.Application.ImportPipeline.Normalization;
using TwinParticles.CheckEngine.Domain.ImportPipeline;
using TwinParticles.CheckEngine.Domain.Oem;

namespace TwinParticles.CheckEngine.Tests.Architecture;

[TestFixture]
public class ImportNormalizationServiceTests
{
    [Test]
    public async Task NormalizeAsync_Should_Normalize_Oem_And_Trim_Fields()
    {
        var service = new ImportNormalizationService(new FakeOemNormalizationService());

        var rows = new List<ImportExtractedRow>
        {
            new()
            {
                RowNumber = 7,
                Fields = new Dictionary<string, string?>
                {
                    ["oem"] = " 11-51-7-586-925 ",
                    ["name"] = "  Oil Filter  "
                }
            }
        };

        var result = await service.NormalizeAsync(rows, CancellationToken.None);

        result.TotalRows.Should().Be(1);
        result.NormalizedRows.Should().Be(1);
        result.Rows.Should().HaveCount(1);
        result.Rows[0].RowNumber.Should().Be(7);
        result.Rows[0].OemNumberRaw.Should().Be("11-51-7-586-925");
        result.Rows[0].OemNumberNormalized.Should().Be("NORM:11-51-7-586-925");
        result.Rows[0].Fields["name"].Should().Be("Oil Filter");
    }

    [Test]
    public async Task NormalizeAsync_Should_Use_Fallback_Oem_Key_When_Primary_Key_Missing()
    {
        var service = new ImportNormalizationService(new FakeOemNormalizationService());

        var rows = new List<ImportExtractedRow>
        {
            new()
            {
                RowNumber = 1,
                Fields = new Dictionary<string, string?>
                {
                    ["partnumber"] = "ABC-123"
                }
            }
        };

        var result = await service.NormalizeAsync(rows, CancellationToken.None);

        result.Rows[0].OemNumberRaw.Should().Be("ABC-123");
        result.Rows[0].OemNumberNormalized.Should().Be("NORM:ABC-123");
    }

    private sealed class FakeOemNormalizationService : IOemNormalizationService
    {
        public string Normalize(string rawNumber)
        {
            return $"NORM:{rawNumber}";
        }
    }
}
