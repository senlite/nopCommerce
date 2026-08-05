using System.Collections.Generic;
using TwinParticles.CheckEngine.Application.ImportPipeline.Orchestration;

namespace TwinParticles.CheckEngine.Application.ImportPipeline.Stages;

public sealed class ImportReviewService
{
    public int Apply(IReadOnlyList<ImportPipelineRowState> rows)
    {
        var reviewRows = 0;

        foreach (var row in rows)
        {
            string? reviewReasonCode = null;

            if (row.IsDuplicate)
            {
                reviewReasonCode = "import.review.duplicate";
            }
            else if (!string.IsNullOrWhiteSpace(row.OemErrorCode))
            {
                reviewReasonCode = "import.review.oem_error";
            }
            else if (row.VehicleMatchConfidence < 0.8m)
            {
                reviewReasonCode = "import.review.vehicle_confidence_low";
            }

            var shouldReview = !string.IsNullOrWhiteSpace(reviewReasonCode);
            row.ReviewStatus = shouldReview ? "Pending" : "Approved";
            row.ReviewReasonCode = shouldReview ? reviewReasonCode : null;

            if (shouldReview)
                reviewRows++;
        }

        return reviewRows;
    }
}
