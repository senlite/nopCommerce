using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using TwinParticles.CheckEngine.Application.Images;
using TwinParticles.CheckEngine.Application.ImportPipeline.Extraction;
using TwinParticles.CheckEngine.Application.ImportPipeline.Normalization;
using TwinParticles.CheckEngine.Application.ImportPipeline.Stages;
using TwinParticles.CheckEngine.Domain.ImportPipeline;
using TwinParticles.CheckEngine.Domain.Performance;
using TwinParticles.CheckEngine.Domain.Security;

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
    private readonly ImageImportOrchestrationService _imageImportOrchestrationService;
    private readonly ICheckEngineAuditService? _auditService;
    private readonly IServiceScopeFactory? _scopeFactory;

    // Retained only for isolated unit tests that intentionally omit a repository. Production always
    // injects IImportPipelineRepository and reads SQL for every admin operation.
    private readonly Dictionary<Guid, ImportPipelineBatchState> _testBatches = [];
    private readonly IImportPipelineRepository? _pipelineRepository;

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
        ImportPublicationService publicationService,
        ImageImportOrchestrationService imageImportOrchestrationService,
        ICheckEngineAuditService? auditService = null,
        IServiceScopeFactory? scopeFactory = null,
        IImportPipelineRepository? pipelineRepository = null)
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
        _imageImportOrchestrationService = imageImportOrchestrationService;
        _auditService = auditService;
        _scopeFactory = scopeFactory;
        _pipelineRepository = pipelineRepository;
    }

    public async Task<ImportPipelineRunResult> RunAsync(ImportPipelineRunRequest request, CancellationToken cancellationToken)
    {
        var batch = new ImportPipelineBatchState
        {
            BatchId = Guid.NewGuid(),
            FileName = request.FileName,
            Format = request.Format,
            DryRun = request.DryRun,
            SourceContent = request.Content,
            SupplierProfileCode = request.SupplierProfileCode,
            EnableAiEnrichment = request.EnableAiEnrichment,
            EnableTranslation = request.EnableTranslation,
            EnableSeoGeneration = request.EnableSeoGeneration,
            Status = "Received",
            CreatedUtc = _clock.UtcNow,
        };

        await PersistBatchAsync(batch, cancellationToken);
        foreach (var stage in PrePublicationStages)
            await ExecuteStageAndCheckpointAsync(batch, stage, dryRun: request.DryRun, cancellationToken);

        return new ImportPipelineRunResult
        {
            BatchId = batch.BatchId,
            Status = batch.Status,
            TotalRows = batch.TotalRows,
            ReviewRows = batch.Rows.Count(row => row.ReviewStatus == "Pending")
        };
    }

    public ImportPipelineBatchState? GetBatch(Guid batchId)
    {
        return GetBatchAsync(batchId, CancellationToken.None).GetAwaiter().GetResult();
    }

    public Task<ImportPipelineBatchState?> GetBatchAsync(Guid batchId, CancellationToken cancellationToken)
    {
        return LoadBatchAsync(batchId, cancellationToken);
    }

    public bool SetReviewStatus(Guid batchId, int rowNumber, string reviewStatus, string actor = "system")
    {
        return SetReviewStatusAsync(batchId, rowNumber, reviewStatus, actor, CancellationToken.None)
            .GetAwaiter().GetResult();
    }

    public async Task<bool> SetReviewStatusAsync(
        Guid batchId,
        int rowNumber,
        string reviewStatus,
        string actor,
        CancellationToken cancellationToken)
    {
        var batch = await LoadBatchAsync(batchId, cancellationToken);
        if (batch is null)
            return false;

        var normalizedReviewStatus = reviewStatus?.Trim();
        if (!string.Equals(normalizedReviewStatus, "Approved", StringComparison.OrdinalIgnoreCase)
            && !string.Equals(normalizedReviewStatus, "Pending", StringComparison.OrdinalIgnoreCase)
            && !string.Equals(normalizedReviewStatus, "Rejected", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var row = batch.Rows.FirstOrDefault(x => x.RowNumber == rowNumber);
        if (row is null)
            return false;

        var before = row.ReviewStatus;
        var canonicalReviewStatus = string.Equals(normalizedReviewStatus, "Approved", StringComparison.OrdinalIgnoreCase)
            ? "Approved"
            : string.Equals(normalizedReviewStatus, "Rejected", StringComparison.OrdinalIgnoreCase)
                ? "Rejected"
                : "Pending";

        row.ReviewStatus = canonicalReviewStatus;
        if (canonicalReviewStatus == "Approved")
            row.ReviewReasonCode = null;
        else if (canonicalReviewStatus == "Rejected")
            row.ReviewReasonCode = "import.review.rejected_by_operator";

        if (_auditService is not null
            && (canonicalReviewStatus == "Approved" || canonicalReviewStatus == "Rejected"))
        {
            var action = canonicalReviewStatus == "Approved" ? "import.approve" : "import.reject";
            await _auditService.AppendAsync(
                    actor,
                    action,
                    "ImportRow",
                    $"{batchId}:{rowNumber}",
                    beforeJson: $"{{\"reviewStatus\":\"{before}\"}}",
                    afterJson: $"{{\"reviewStatus\":\"{canonicalReviewStatus}\"}}",
                    cancellationToken);
        }

        await PersistBatchAsync(batch, cancellationToken);
        return true;
    }

    public ImportPublicationResult Publish(Guid batchId, bool dryRun)
    {
        return PublishAsync(batchId, dryRun, CancellationToken.None).GetAwaiter().GetResult();
    }

    public async Task<ImportPublicationResult> PublishAsync(Guid batchId, bool dryRun, CancellationToken cancellationToken)
    {
        var batch = await LoadBatchAsync(batchId, cancellationToken);
        if (batch is null)
        {
            return new ImportPublicationResult
            {
                DryRun = dryRun,
                PublishedRows = 0,
                FailedRows = 0
            };
        }

        var result = await ExecutePublicationAsync(batch, dryRun, cancellationToken);

        if (_auditService is not null && !dryRun)
        {
            await _auditService.AppendAsync(
                "system",
                "import.publish",
                "ImportBatch",
                batchId.ToString(),
                beforeJson: null,
                afterJson: $"{{\"publishedRows\":{result.PublishedRows},\"failedRows\":{result.FailedRows}}}",
                cancellationToken);
        }

        return result;
    }

    public async Task<ImportPipelineStageRunResult> RerunStageAsync(
        Guid batchId,
        ImportPipelineStage stage,
        CancellationToken cancellationToken)
    {
        var batch = await LoadBatchAsync(batchId, cancellationToken)
                    ?? throw new InvalidOperationException("import.batch_not_found");
        if (batch.Status == "Committed" && stage != ImportPipelineStage.Publish)
            throw new InvalidOperationException("import.committed_batch_not_rerunnable");

        if (_auditService is not null)
        {
            await _auditService.AppendAsync(
                "system",
                "import.rerun_stage",
                "ImportBatch",
                batchId.ToString(),
                beforeJson: null,
                afterJson: $"{{\"stage\":\"{stage}\"}}",
                cancellationToken);
        }

        if (stage == ImportPipelineStage.Publish)
            await ExecutePublicationAsync(batch, batch.DryRun, cancellationToken);
        else
        {
            await ExecuteStageAndCheckpointAsync(batch, stage, batch.DryRun, cancellationToken);
            if (stage != ImportPipelineStage.Review &&
                batch.CompletedStages.Contains(ImportPipelineStage.Review.ToString()))
            {
                batch.Status = "Review";
                await PersistBatchAsync(batch, cancellationToken);
            }
        }

        return new ImportPipelineStageRunResult
        {
            BatchId = batchId,
            Stage = stage,
            Status = batch.Status,
            TotalRows = batch.TotalRows,
            FailedRows = batch.Rows.Count(row => !string.IsNullOrWhiteSpace(row.LastStageError))
        };
    }

    private async Task PersistBatchAsync(ImportPipelineBatchState batch, CancellationToken cancellationToken)
    {
        if (_pipelineRepository is not null)
        {
            await PersistWithRepositoryAsync(_pipelineRepository, batch, cancellationToken);
            return;
        }

        if (_scopeFactory is not null)
        {
            using var scope = _scopeFactory.CreateScope();
            var pipelineRepository = scope.ServiceProvider.GetService<IImportPipelineRepository>();
            if (pipelineRepository is not null)
            {
                await PersistWithRepositoryAsync(pipelineRepository, batch, cancellationToken);
                return;
            }
        }

        _testBatches[batch.BatchId] = batch;
    }

    private static readonly ImportPipelineStage[] PrePublicationStages =
    [
        ImportPipelineStage.Extract,
        ImportPipelineStage.Normalize,
        ImportPipelineStage.Deduplicate,
        ImportPipelineStage.OemMatch,
        ImportPipelineStage.VehicleMatch,
        ImportPipelineStage.Enrich,
        ImportPipelineStage.Translate,
        ImportPipelineStage.SeoGenerate,
        ImportPipelineStage.Categorize,
        ImportPipelineStage.ImageAssign,
        ImportPipelineStage.Review
    ];

    private async Task ExecuteStageAndCheckpointAsync(
        ImportPipelineBatchState batch,
        ImportPipelineStage stage,
        bool dryRun,
        CancellationToken cancellationToken)
    {
        batch.CurrentStage = stage.ToString();
        batch.Status = StageStatus(stage);
        batch.ErrorSummary = null;
        await PersistBatchAsync(batch, cancellationToken);

        try
        {
            await ExecuteStageAsync(batch, stage, dryRun, cancellationToken);
            batch.CompletedStages.Add(stage.ToString());
            foreach (var row in batch.Rows)
            {
                row.CompletedStages.Add(stage.ToString());
                row.LastStageError = null;
            }

            batch.TotalRows = batch.Rows.Count;
            batch.Status = stage == ImportPipelineStage.Review ? "Review" : StageStatus(stage);
            await PersistBatchAsync(batch, cancellationToken);
        }
        catch (Exception exception)
        {
            batch.Status = "Failed";
            batch.ErrorSummary = $"{stage}: {exception.Message}";
            foreach (var row in batch.Rows)
                row.LastStageError = batch.ErrorSummary;
            await PersistBatchAsync(batch, CancellationToken.None);
            throw;
        }
    }

    private async Task ExecuteStageAsync(
        ImportPipelineBatchState batch,
        ImportPipelineStage stage,
        bool dryRun,
        CancellationToken cancellationToken)
    {
        switch (stage)
        {
            case ImportPipelineStage.Extract:
                var extraction = await _extractionService.ExtractAsync(new ImportExtractionRequest
                {
                    Format = batch.Format,
                    FileName = batch.FileName,
                    Content = batch.SourceContent,
                    SupplierProfileCode = batch.SupplierProfileCode
                }, cancellationToken);
                if (!extraction.Success)
                    throw new InvalidOperationException(extraction.ErrorCode ?? "import.extraction_failed");
                batch.Rows.Clear();
                batch.Rows.AddRange(extraction.Rows.Select(row => new ImportPipelineRowState
                {
                    RowNumber = row.RowNumber,
                    Fields = row.Fields
                }));
                break;

            case ImportPipelineStage.Normalize:
                var normalized = await _normalizationService.NormalizeAsync(
                    batch.Rows.Select(ToExtractedRow).ToList(),
                    cancellationToken);
                foreach (var normalizedRow in normalized.Rows)
                {
                    var target = batch.Rows.Single(row => row.RowNumber == normalizedRow.RowNumber);
                    target.Fields = normalizedRow.Fields;
                    target.OemNumberNormalized = normalizedRow.OemNumberNormalized;
                }
                break;

            case ImportPipelineStage.Deduplicate:
                var duplicates = _duplicateDetectionService.DetectDuplicateRowNumbers(ToNormalizedRows(batch.Rows));
                foreach (var row in batch.Rows)
                    row.IsDuplicate = duplicates.Contains(row.RowNumber);
                break;

            case ImportPipelineStage.OemMatch:
                var matches = await _oemMatchingService.MatchAsync(ToNormalizedRows(batch.Rows), cancellationToken);
                foreach (var row in batch.Rows)
                {
                    var match = matches[row.RowNumber];
                    row.OemNumberId = match.Success ? match.OemNumberId : null;
                    row.OemErrorCode = match.Success ? null : match.ErrorCode;
                }
                break;

            case ImportPipelineStage.VehicleMatch:
                _vehicleMatchingService.Apply(batch.Rows);
                break;
            case ImportPipelineStage.Enrich:
                _aiEnrichmentHookService.Apply(batch.Rows, batch.EnableAiEnrichment);
                break;
            case ImportPipelineStage.Translate:
                _translationHookService.Apply(batch.Rows, batch.EnableTranslation);
                break;
            case ImportPipelineStage.SeoGenerate:
                _seoGenerationHookService.Apply(batch.Rows, batch.EnableSeoGeneration);
                break;
            case ImportPipelineStage.Categorize:
                _categorizationService.Apply(batch.Rows);
                break;
            case ImportPipelineStage.ImageAssign:
                _imageAssignmentService.Apply(batch.Rows);
                break;
            case ImportPipelineStage.Review:
                _reviewService.Apply(batch.Rows);
                break;
            case ImportPipelineStage.Publish:
                await ExecutePublicationAsync(batch, dryRun, cancellationToken);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(stage), stage, "Unknown import stage.");
        }
    }

    private async Task<ImportPublicationResult> ExecutePublicationAsync(
        ImportPipelineBatchState batch,
        bool dryRun,
        CancellationToken cancellationToken)
    {
        batch.CurrentStage = ImportPipelineStage.Publish.ToString();
        batch.Status = "Publishing";
        await PersistBatchAsync(batch, cancellationToken);

        var result = await _publicationService.PublishAsync(batch.Rows, dryRun, cancellationToken);
        if (!dryRun)
        {
            foreach (var row in batch.Rows.Where(row => row.IsPublished))
                await _imageImportOrchestrationService.ApplyAsync(row, cancellationToken);
        }

        batch.PublishedRows = result.PublishedRows;
        batch.FailedRows = result.FailedRows;
        batch.Status = dryRun ? "Review" : "Committed";
        batch.CompletedStages.Add(ImportPipelineStage.Publish.ToString());
        foreach (var row in batch.Rows)
            row.CompletedStages.Add(ImportPipelineStage.Publish.ToString());
        await PersistBatchAsync(batch, cancellationToken);
        return result;
    }

    private async Task<ImportPipelineBatchState?> LoadBatchAsync(Guid batchId, CancellationToken cancellationToken)
    {
        if (_pipelineRepository is not null)
            return await LoadWithRepositoryAsync(_pipelineRepository, batchId, cancellationToken);

        if (_scopeFactory is not null)
        {
            using var scope = _scopeFactory.CreateScope();
            var repository = scope.ServiceProvider.GetService<IImportPipelineRepository>();
            if (repository is not null)
                return await LoadWithRepositoryAsync(repository, batchId, cancellationToken);
        }

        return _testBatches.TryGetValue(batchId, out var batch) ? batch : null;
    }

    private async Task PersistWithRepositoryAsync(
        IImportPipelineRepository repository,
        ImportPipelineBatchState batch,
        CancellationToken cancellationToken)
    {
        var entity = ImportPipelineStateMapper.ToEntity(batch, _clock.UtcNow.UtcDateTime);
        await repository.UpsertBatchAsync(entity, cancellationToken);
        batch.SqlBatchId = entity.Id;
        await repository.ReplaceRowsAsync(
            entity.Id,
            batch.Rows.Select(row => ImportPipelineStateMapper.ToEntity(entity.Id, row)).ToList(),
            cancellationToken);
    }

    private static async Task<ImportPipelineBatchState?> LoadWithRepositoryAsync(
        IImportPipelineRepository repository,
        Guid batchId,
        CancellationToken cancellationToken)
    {
        var entity = await repository.GetBatchByCorrelationIdAsync(batchId, cancellationToken);
        if (entity is null)
            return null;
        var rows = await repository.GetRowsAsync(entity.Id, cancellationToken);
        return ImportPipelineStateMapper.FromEntities(entity, rows);
    }

    private static ImportExtractedRow ToExtractedRow(ImportPipelineRowState row)
        => new() { RowNumber = row.RowNumber, Fields = row.Fields };

    private static IReadOnlyList<ImportNormalizedRow> ToNormalizedRows(IEnumerable<ImportPipelineRowState> rows)
        => rows.Select(row => new ImportNormalizedRow
        {
            RowNumber = row.RowNumber,
            OemNumberRaw = GetRawOem(row.Fields),
            OemNumberNormalized = row.OemNumberNormalized,
            Fields = row.Fields
        }).ToList();

    private static string? GetRawOem(IReadOnlyDictionary<string, string?> fields)
    {
        foreach (var key in new[] { "oem", "oemnumber", "partnumber" })
        {
            if (fields.TryGetValue(key, out var value))
                return value;
        }

        return null;
    }

    private static string StageStatus(ImportPipelineStage stage)
        => stage switch
        {
            ImportPipelineStage.Extract or ImportPipelineStage.Normalize => "Parsing",
            ImportPipelineStage.Deduplicate or ImportPipelineStage.OemMatch or ImportPipelineStage.VehicleMatch => "Matching",
            ImportPipelineStage.Review => "Review",
            ImportPipelineStage.Publish => "Publishing",
            _ => "Processing"
        };
}
