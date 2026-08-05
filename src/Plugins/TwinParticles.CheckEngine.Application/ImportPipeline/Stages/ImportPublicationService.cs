using System.Collections.Generic;
using TwinParticles.CheckEngine.Application.ImportPipeline.Orchestration;

namespace TwinParticles.CheckEngine.Application.ImportPipeline.Stages;

public sealed class ImportPublicationService
{
    public ImportPublicationResult Publish(IReadOnlyList<ImportPipelineRowState> rows, bool dryRun)
    {
        if (dryRun)
        {
            return new ImportPublicationResult
            {
                DryRun = true,
                PublishedRows = 0,
                FailedRows = 0
            };
        }

        var published = 0;
        var failed = 0;

        foreach (var row in rows)
        {
            if (row.ReviewStatus == "Rejected")
            {
                row.IsPublished = false;
                row.PublishError = "import.review_rejected";
                failed++;
                continue;
            }

            if (row.ReviewStatus != "Approved")
            {
                row.IsPublished = false;
                row.PublishError = "import.review_pending";
                failed++;
                continue;
            }

            row.IsPublished = true;
            row.PublishError = null;
            published++;
        }

        return new ImportPublicationResult
        {
            DryRun = false,
            PublishedRows = published,
            FailedRows = failed
        };
    }
}
