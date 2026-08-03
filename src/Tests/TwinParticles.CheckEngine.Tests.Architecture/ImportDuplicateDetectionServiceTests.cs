using System.Collections.Generic;
using FluentAssertions;
using NUnit.Framework;
using TwinParticles.CheckEngine.Application.ImportPipeline.Stages;
using TwinParticles.CheckEngine.Domain.ImportPipeline;

namespace TwinParticles.CheckEngine.Tests.Architecture;

[TestFixture]
public class ImportDuplicateDetectionServiceTests
{
    [Test]
    public void DetectDuplicateRowNumbers_Should_Flag_Subsequent_Duplicates()
    {
        var service = new ImportDuplicateDetectionService();

        var rows = new List<ImportNormalizedRow>
        {
            new() { RowNumber = 1, OemNumberNormalized = "11517586925", Fields = new Dictionary<string, string?> { ["name"] = "Oil Filter" } },
            new() { RowNumber = 2, OemNumberNormalized = "11517586925", Fields = new Dictionary<string, string?> { ["name"] = "Oil Filter" } },
            new() { RowNumber = 3, OemNumberNormalized = "11517586925", Fields = new Dictionary<string, string?> { ["name"] = "Cabin Filter" } }
        };

        var duplicates = service.DetectDuplicateRowNumbers(rows);

        duplicates.Should().BeEquivalentTo([2]);
    }
}
