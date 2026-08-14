using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TwinParticles.CheckEngine.Application.ImportPipeline.Orchestration;
using TwinParticles.CheckEngine.Domain.ImportPipeline;

namespace TwinParticles.CheckEngine.Application.ImportPipeline.Stages;

public sealed class ImportPublicationService
{
    private readonly IImportProductPublisher? _productPublisher;

    public ImportPublicationService(IImportProductPublisher? productPublisher = null)
    {
        _productPublisher = productPublisher;
    }

    public ImportPublicationResult Publish(IReadOnlyList<ImportPipelineRowState> rows, bool dryRun)
        => PublishAsync(rows, dryRun, CancellationToken.None).GetAwaiter().GetResult();

    public async Task<ImportPublicationResult> PublishAsync(
        IReadOnlyList<ImportPipelineRowState> rows,
        bool dryRun,
        CancellationToken cancellationToken)
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
            // A row merged into its duplicate original does not become a distinct product; it is a
            // resolved decision, not a failure.
            if (row.DuplicateDecision == "Merge")
            {
                row.IsPublished = false;
                row.PublishError = null;
                continue;
            }

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

            if (_productPublisher is not null)
            {
                var result = await _productPublisher.PublishAsync(row.Fields, row.OemNumberId, cancellationToken);
                if (!result.Success)
                {
                    row.IsPublished = false;
                    row.PublishError = result.ErrorCode ?? "import.publish_failed";
                    failed++;
                    continue;
                }

                if (result.ProductId is > 0)
                {
                    row.Fields = new Dictionary<string, string?>(row.Fields)
                    {
                        ["publishedProductId"] = result.ProductId.Value.ToString()
                    };
                }
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
