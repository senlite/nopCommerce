using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using LinqToDB.Data;
using Nop.Data;
using TwinParticles.CheckEngine.Domain.ImportPipeline;

namespace TwinParticles.CheckEngine.Infrastructure.ImportPipeline;

public sealed class SqlImportPipelineRepository : IImportPipelineRepository
{
    private readonly INopDataProvider _dataProvider;

    public SqlImportPipelineRepository(INopDataProvider dataProvider)
    {
        _dataProvider = dataProvider;
    }

    public async Task<ImportBatch?> GetBatchAsync(int batchId, CancellationToken cancellationToken)
    {
        var rows = await _dataProvider.QueryAsync<ImportBatchRow>(
            @"SELECT Id, CorrelationId, FileName, SourceFormatId, Status, UploadedByCustomerId, RowCount, ErrorSummary,
       SourceContent, RunOptionsJson, CurrentStage, CompletedStagesCsv, CreatedUtc, UpdatedUtc
FROM TP_CE_ImportBatch WHERE Id = @id",
            new DataParameter("id", batchId));

        return rows.Select(MapBatch).FirstOrDefault();
    }

    public async Task<ImportBatch?> GetBatchByCorrelationIdAsync(Guid correlationId, CancellationToken cancellationToken)
    {
        var rows = await _dataProvider.QueryAsync<ImportBatchRow>(
            @"SELECT Id, CorrelationId, FileName, SourceFormatId, Status, UploadedByCustomerId, RowCount, ErrorSummary,
       SourceContent, RunOptionsJson, CurrentStage, CompletedStagesCsv, CreatedUtc, UpdatedUtc
FROM TP_CE_ImportBatch WHERE CorrelationId = @correlationId",
            new DataParameter("correlationId", correlationId));

        return rows.Select(MapBatch).FirstOrDefault();
    }

    public async Task UpsertBatchAsync(ImportBatch batch, CancellationToken cancellationToken)
    {
        if (batch.Id > 0)
        {
            var updated = await _dataProvider.ExecuteNonQueryAsync(
                @"UPDATE TP_CE_ImportBatch
SET CorrelationId = @correlationId,
    FileName = @fileName,
    SourceFormatId = @sourceFormatId,
    Status = @status,
    UploadedByCustomerId = @uploadedByCustomerId,
    RowCount = @rowCount,
    ErrorSummary = @errorSummary,
    SourceContent = @sourceContent,
    RunOptionsJson = @runOptionsJson,
    CurrentStage = @currentStage,
    CompletedStagesCsv = @completedStagesCsv,
    UpdatedUtc = @updatedUtc
WHERE Id = @id",
                new DataParameter("correlationId", batch.CorrelationId),
                new DataParameter("fileName", batch.FileName),
                new DataParameter("sourceFormatId", (int)batch.SourceFormat),
                new DataParameter("status", batch.Status),
                new DataParameter("uploadedByCustomerId", batch.UploadedByCustomerId),
                new DataParameter("rowCount", batch.RowCount),
                new DataParameter("errorSummary", batch.ErrorSummary),
                new DataParameter("sourceContent", batch.SourceContent),
                new DataParameter("runOptionsJson", batch.RunOptionsJson),
                new DataParameter("currentStage", batch.CurrentStage),
                new DataParameter("completedStagesCsv", batch.CompletedStagesCsv),
                new DataParameter("updatedUtc", batch.UpdatedUtc),
                new DataParameter("id", batch.Id));

            if (updated > 0)
                return;
        }

        var inserted = await _dataProvider.QueryAsync<ScalarIntRow>(
            @"INSERT INTO TP_CE_ImportBatch
(CorrelationId, FileName, SourceFormatId, Status, UploadedByCustomerId, RowCount, ErrorSummary, SourceContent, RunOptionsJson, CurrentStage, CompletedStagesCsv, CreatedUtc, UpdatedUtc)
VALUES
(@correlationId, @fileName, @sourceFormatId, @status, @uploadedByCustomerId, @rowCount, @errorSummary, @sourceContent, @runOptionsJson, @currentStage, @completedStagesCsv, @createdUtc, @updatedUtc);
SELECT CAST(SCOPE_IDENTITY() as int) AS Value;",
            new DataParameter("correlationId", batch.CorrelationId),
            new DataParameter("fileName", batch.FileName),
            new DataParameter("sourceFormatId", (int)batch.SourceFormat),
            new DataParameter("status", batch.Status),
            new DataParameter("uploadedByCustomerId", batch.UploadedByCustomerId),
            new DataParameter("rowCount", batch.RowCount),
            new DataParameter("errorSummary", batch.ErrorSummary),
            new DataParameter("sourceContent", batch.SourceContent),
            new DataParameter("runOptionsJson", batch.RunOptionsJson),
            new DataParameter("currentStage", batch.CurrentStage),
            new DataParameter("completedStagesCsv", batch.CompletedStagesCsv),
            new DataParameter("createdUtc", batch.CreatedUtc),
            new DataParameter("updatedUtc", batch.UpdatedUtc));

        batch.Id = inserted.Single().Value;
    }

    public async Task<IReadOnlyList<ImportRow>> GetRowsAsync(int batchId, CancellationToken cancellationToken)
    {
        var rows = await _dataProvider.QueryAsync<ImportRow>(
            @"SELECT Id, BatchId, RowNumber, RawPayload, NormalizedOem, MatchedOemNumberId, ProposedProductId, ProposedFitmentJson, Confidence, ReviewStatus, ReviewNote,
       PipelineStateJson, CompletedStagesCsv, LastStageError
FROM TP_CE_ImportRow
WHERE BatchId = @batchId
ORDER BY RowNumber",
            new DataParameter("batchId", batchId));

        return rows.ToList();
    }

    public async Task ReplaceRowsAsync(int batchId, IReadOnlyList<ImportRow> rows, CancellationToken cancellationToken)
    {
        var existing = await _dataProvider.QueryAsync<ImportRowNumber>(
            "SELECT RowNumber FROM TP_CE_ImportRow WHERE BatchId = @batchId",
            new DataParameter("batchId", batchId));
        var retained = rows.Select(row => row.RowNumber).ToHashSet();

        foreach (var row in rows)
        {
            var parameters = new[]
            {
                new DataParameter("batchId", batchId),
                new DataParameter("rowNumber", row.RowNumber),
                new DataParameter("rawPayload", row.RawPayload ?? JsonSerializer.Serialize(new { })),
                new DataParameter("normalizedOem", row.NormalizedOem),
                new DataParameter("matchedOemNumberId", row.MatchedOemNumberId),
                new DataParameter("proposedProductId", row.ProposedProductId),
                new DataParameter("proposedFitmentJson", row.ProposedFitmentJson),
                new DataParameter("confidence", row.Confidence),
                new DataParameter("reviewStatus", row.ReviewStatus),
                new DataParameter("reviewNote", row.ReviewNote),
                new DataParameter("pipelineStateJson", row.PipelineStateJson),
                new DataParameter("completedStagesCsv", row.CompletedStagesCsv),
                new DataParameter("lastStageError", row.LastStageError)
            };

            var updated = await _dataProvider.ExecuteNonQueryAsync(
                @"UPDATE TP_CE_ImportRow
SET RawPayload = @rawPayload,
    NormalizedOem = @normalizedOem,
    MatchedOemNumberId = @matchedOemNumberId,
    ProposedProductId = @proposedProductId,
    ProposedFitmentJson = @proposedFitmentJson,
    Confidence = @confidence,
    ReviewStatus = @reviewStatus,
    ReviewNote = @reviewNote,
    PipelineStateJson = @pipelineStateJson,
    CompletedStagesCsv = @completedStagesCsv,
    LastStageError = @lastStageError
WHERE BatchId = @batchId AND RowNumber = @rowNumber",
                parameters);

            if (updated > 0)
                continue;

            await _dataProvider.ExecuteNonQueryAsync(
                @"INSERT INTO TP_CE_ImportRow
(BatchId, RowNumber, RawPayload, NormalizedOem, MatchedOemNumberId, ProposedProductId, ProposedFitmentJson, Confidence, ReviewStatus, ReviewNote, PipelineStateJson, CompletedStagesCsv, LastStageError)
VALUES
(@batchId, @rowNumber, @rawPayload, @normalizedOem, @matchedOemNumberId, @proposedProductId, @proposedFitmentJson, @confidence, @reviewStatus, @reviewNote, @pipelineStateJson, @completedStagesCsv, @lastStageError)",
                parameters);
        }

        foreach (var stale in existing.Where(item => !retained.Contains(item.RowNumber)))
        {
            await _dataProvider.ExecuteNonQueryAsync(
                "DELETE FROM TP_CE_ImportRow WHERE BatchId = @batchId AND RowNumber = @rowNumber",
                new DataParameter("batchId", batchId),
                new DataParameter("rowNumber", stale.RowNumber));
        }
    }

    private static ImportBatch MapBatch(ImportBatchRow row)
        => new()
        {
            Id = row.Id,
            CorrelationId = row.CorrelationId,
            FileName = row.FileName,
            SourceFormat = (ImportSourceFormat)row.SourceFormatId,
            Status = row.Status,
            UploadedByCustomerId = row.UploadedByCustomerId,
            RowCount = row.RowCount,
            ErrorSummary = row.ErrorSummary,
            SourceContent = row.SourceContent ?? [],
            RunOptionsJson = row.RunOptionsJson,
            CurrentStage = row.CurrentStage,
            CompletedStagesCsv = row.CompletedStagesCsv,
            CreatedUtc = row.CreatedUtc,
            UpdatedUtc = row.UpdatedUtc
        };

    private sealed class ScalarIntRow
    {
        public int Value { get; set; }
    }

    private sealed class ImportRowNumber
    {
        public int RowNumber { get; set; }
    }

    private sealed class ImportBatchRow
    {
        public int Id { get; set; }
        public Guid? CorrelationId { get; set; }
        public string FileName { get; set; } = string.Empty;
        public int SourceFormatId { get; set; }
        public string Status { get; set; } = string.Empty;
        public int? UploadedByCustomerId { get; set; }
        public int RowCount { get; set; }
        public string? ErrorSummary { get; set; }
        public byte[]? SourceContent { get; set; }
        public string? RunOptionsJson { get; set; }
        public string? CurrentStage { get; set; }
        public string? CompletedStagesCsv { get; set; }
        public DateTime CreatedUtc { get; set; }
        public DateTime UpdatedUtc { get; set; }
    }
}
