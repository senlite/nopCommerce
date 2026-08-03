using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TwinParticles.CheckEngine.Application.ImportPipeline.Extraction;
using TwinParticles.CheckEngine.Application.ImportPipeline.Normalization;
using TwinParticles.CheckEngine.Application.ImportPipeline.Stages;
using TwinParticles.CheckEngine.Domain.ImportPipeline;
using TwinParticles.CheckEngine.Domain.Performance;

namespace TwinParticles.CheckEngine.Application.ImportPipeline.Orchestration;

public sealed class ImportPipelineOrchestratorService
{
    private readonly ICheckEngineClock _clock;
    private readonly ImportExtractionService _extractionService;
    private readonly ImportNormalizationService _normalizationService;
    private readonly ImportDuplicateDetectionService _duplicateDetectionService;
    private readonly ImportOemMatchingService _oemMatchingService;
    private readonly ImportVehicleMatchingService _vehicleMatchingService;
    private readonly ImportAiEnrichmentHookService _aiEnrichmentHookService;
    private readonly ImportTranslationHookService _translationHookService;
    private readonly ImportSeoGenerationHookService _seoGenerationHookService;
    private readonly ImportCategorizationService _categorizationService;
    private readonly ImportImageAssignmentService _imageAssignmentService;
    private readonly ImportReviewService _reviewService;
    private readonly ImportPublicationService _publicationService;

    private readonly ConcurrentDictionary<Guid, ImportPipelineBatchState> _batches = new();

    public ImportPipelineOrchestratorService(
        ICheckEngineClock clock,
        ImportExtractionService extractionService,
        ImportNormalizationService normalizationService,
        ImportDuplicateDetectionService duplicateDetectionService,
        ImportOemMatchingService oemMatchingService,
        ImportVehicleMatchingService vehicleMatchingService,
        ImportAiEnrichmentHookService aiEnrichmentHookService,
        ImportTranslationHookService translationHookService,
        ImportSeoGenerationHookService seoGenerationHookService,
        ImportCategorizationService categorizationService,
        ImportImageAssignmentService imageAssignmentService,
        ImportReviewService reviewService,
        ImportPublicationService publicationService)
    {
        _clock = clock;
        _extractionService = extractionService;
        _normalizationService = normalizationService;
        _duplicateDetectionService = duplicateDetectionService;
        _oemMatchingService = oemMatchingService;
        _vehicleMatchingService = vehicleMatchingService;
        _aiEnrichmentHookService = aiEnrichmentHookService;
        _translationHookService = translationHookService;
        _seoGenerationHookService = seoGenerationHookService;
        _categorizationService = categorizationService;
        _imageAssignmentService = imageAssignmentService;
        _reviewService = reviewService;
        _publicationService = publicationService;
    }

    public async Task<ImportPipelineRunResult> RunAsync(ImportPipelineRunRequest request, CancellationToken cancellationToken)
    {
        var batchId = Guid.NewGuid();

        var extraction = await _extractionService.ExtractAsync(new ImportExtractionRequest
        {
            Format = request.Format,
            FileName = request.FileName,
            Content = request.Content,
            SupplierProfileCode = request.SupplierProfileCode
        }, cancellationToken);

        var extractedRows = extraction.Success ? extraction.Rows : [];

        var normalization = await _normalizationService.NormalizeAsync(extractedRows, cancellationToken);
        var duplicateRows = _duplicateDetectionService.DetectDuplicateRowNumbers(normalization.Rows);
        var oemMatches = await _oemMatchingService.MatchAsync(normalization.Rows, cancellationToken);

        var rows = normalization.Rows.Select(row =>
        {
            var oem = oemMatches[row.RowNumber];
            return new ImportPipelineRowState
            {
                RowNumber = row.RowNumber,
                Fields = row.Fields,
                OemNumberNormalized = row.OemNumberNormalized,
                OemNumberId = oem.Success ? oem.OemNumberId : null,
                OemErrorCode = oem.Success ? null : oem.ErrorCode,
                IsDuplicate = duplicateRows.Contains(row.RowNumber)
            };
        }).ToList();

        _vehicleMatchingService.Apply(rows);
        _aiEnrichmentHookService.Apply(rows, request.EnableAiEnrichment);
        _translationHookService.Apply(rows, request.EnableTranslation);
        _seoGenerationHookService.Apply(rows, request.EnableSeoGeneration);
        _categorizationService.Apply(rows);
        _imageAssignmentService.Apply(rows);
        var reviewRows = _reviewService.Apply(rows);

        var batch = new ImportPipelineBatchState
        {
            BatchId = batchId,
            FileName = request.FileName,
            Format = request.Format,
            DryRun = request.DryRun,
            Status = "Review",
            CreatedUtc = _clock.UtcNow,
            TotalRows = rows.Count,
            Rows = rows
        };

        _batches[batchId] = batch;

        return new ImportPipelineRunResult
        {
            BatchId = batchId,
            Status = batch.Status,
            TotalRows = rows.Count,
            ReviewRows = reviewRows
        };
    }

    public ImportPipelineBatchState? GetBatch(Guid batchId)
    {
        return _batches.TryGetValue(batchId, out var state) ? state : null;
    }

    public bool SetReviewStatus(Guid batchId, int rowNumber, string reviewStatus)
    {
        if (!_batches.TryGetValue(batchId, out var batch))
            return false;

        var row = batch.Rows.FirstOrDefault(x => x.RowNumber == rowNumber);
        if (row is null)
            return false;

        row.ReviewStatus = reviewStatus;
        return true;
    }

    public ImportPublicationResult Publish(Guid batchId, bool dryRun)
    {
        if (!_batches.TryGetValue(batchId, out var batch))
        {
            return new ImportPublicationResult
            {
                DryRun = dryRun,
                PublishedRows = 0,
                FailedRows = 0
            };
        }

        var result = _publicationService.Publish(batch.Rows, dryRun);
        batch.PublishedRows = result.PublishedRows;
        batch.FailedRows = result.FailedRows;
        batch.Status = dryRun ? "Review" : "Committed";

        return result;
    }
}
