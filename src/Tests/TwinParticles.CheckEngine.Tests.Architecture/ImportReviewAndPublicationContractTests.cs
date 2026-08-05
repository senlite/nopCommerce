using System.Collections.Generic;
using FluentAssertions;
using NUnit.Framework;
using TwinParticles.CheckEngine.Application.ImportPipeline.Orchestration;
using TwinParticles.CheckEngine.Application.ImportPipeline.Stages;

namespace TwinParticles.CheckEngine.Tests.Architecture;

[TestFixture]
public class ImportReviewAndPublicationContractTests
{
    [Test]
    public void Apply_Should_Set_Review_Status_And_ReasonCode_By_Risk_Signal()
    {
        var service = new ImportReviewService();

        var rows = new List<ImportPipelineRowState>
        {
            new() { RowNumber = 1, IsDuplicate = true, VehicleMatchConfidence = 1.0m },
            new() { RowNumber = 2, OemErrorCode = "import.oem_missing", VehicleMatchConfidence = 1.0m },
            new() { RowNumber = 3, VehicleMatchConfidence = 0.45m },
            new() { RowNumber = 4, VehicleMatchConfidence = 1.0m }
        };

        var reviewRows = service.Apply(rows);

        reviewRows.Should().Be(3);

        rows[0].ReviewStatus.Should().Be("Pending");
        rows[0].ReviewReasonCode.Should().Be("import.review.duplicate");

        rows[1].ReviewStatus.Should().Be("Pending");
        rows[1].ReviewReasonCode.Should().Be("import.review.oem_error");

        rows[2].ReviewStatus.Should().Be("Pending");
        rows[2].ReviewReasonCode.Should().Be("import.review.vehicle_confidence_low");

        rows[3].ReviewStatus.Should().Be("Approved");
        rows[3].ReviewReasonCode.Should().BeNull();
    }

    [Test]
    public void Publish_Should_Respect_Review_Status_Contract_And_Error_Codes()
    {
        var service = new ImportPublicationService();

        var rows = new List<ImportPipelineRowState>
        {
            new() { RowNumber = 1, ReviewStatus = "Approved" },
            new() { RowNumber = 2, ReviewStatus = "Pending" },
            new() { RowNumber = 3, ReviewStatus = "Rejected" }
        };

        var result = service.Publish(rows, dryRun: false);

        result.PublishedRows.Should().Be(1);
        result.FailedRows.Should().Be(2);

        rows[0].IsPublished.Should().BeTrue();
        rows[0].PublishError.Should().BeNull();

        rows[1].IsPublished.Should().BeFalse();
        rows[1].PublishError.Should().Be("import.review_pending");

        rows[2].IsPublished.Should().BeFalse();
        rows[2].PublishError.Should().Be("import.review_rejected");
    }
}
