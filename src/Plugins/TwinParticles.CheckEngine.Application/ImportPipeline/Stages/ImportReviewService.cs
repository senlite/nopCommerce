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
            var shouldReview = row.IsDuplicate
                               || !string.IsNullOrWhiteSpace(row.OemErrorCode)
                               || row.VehicleMatchConfidence < 0.8m;

            row.ReviewStatus = shouldReview ? "Pending" : "Approved";
            if (shouldReview)
                reviewRows++;
        }

        return reviewRows;
    }
}
