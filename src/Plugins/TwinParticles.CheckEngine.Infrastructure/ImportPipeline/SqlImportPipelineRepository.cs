using System.Collections.Generic;
using System.Linq;
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
            @"SELECT Id, FileName, SourceFormatId, Status, UploadedByCustomerId, RowCount, ErrorSummary, CreatedUtc, UpdatedUtc
FROM TP_CE_ImportBatch WHERE Id = @id",
            new DataParameter("id", batchId));

        return rows.Select(MapBatch).FirstOrDefault();
    }

    public async Task UpsertBatchAsync(ImportBatch batch, CancellationToken cancellationToken)
    {
        if (batch.Id > 0)
        {
            var updated = await _dataProvider.ExecuteNonQueryAsync(
                @"UPDATE TP_CE_ImportBatch
SET FileName = @fileName,
    SourceFormatId = @sourceFormatId,
    Status = @status,
    UploadedByCustomerId = @uploadedByCustomerId,
    RowCount = @rowCount,
    ErrorSummary = @errorSummary,
    UpdatedUtc = @updatedUtc
WHERE Id = @id",
                new DataParameter("fileName", batch.FileName),
                new DataParameter("sourceFormatId", (int)batch.SourceFormat),
                new DataParameter("status", batch.Status),
                new DataParameter("uploadedByCustomerId", batch.UploadedByCustomerId),
                new DataParameter("rowCount", batch.RowCount),
                new DataParameter("errorSummary", batch.ErrorSummary),
                new DataParameter("updatedUtc", batch.UpdatedUtc),
                new DataParameter("id", batch.Id));

            if (updated > 0)
                return;
        }

        var inserted = await _dataProvider.QueryAsync<ScalarIntRow>(
            @"INSERT INTO TP_CE_ImportBatch
(FileName, SourceFormatId, Status, UploadedByCustomerId, RowCount, ErrorSummary, CreatedUtc, UpdatedUtc)
VALUES
(@fileName, @sourceFormatId, @status, @uploadedByCustomerId, @rowCount, @errorSummary, @createdUtc, @updatedUtc);
SELECT CAST(SCOPE_IDENTITY() as int) AS Value;",
            new DataParameter("fileName", batch.FileName),
            new DataParameter("sourceFormatId", (int)batch.SourceFormat),
            new DataParameter("status", batch.Status),
            new DataParameter("uploadedByCustomerId", batch.UploadedByCustomerId),
            new DataParameter("rowCount", batch.RowCount),
            new DataParameter("errorSummary", batch.ErrorSummary),
            new DataParameter("createdUtc", batch.CreatedUtc),
            new DataParameter("updatedUtc", batch.UpdatedUtc));

        batch.Id = inserted.Single().Value;
    }

    public async Task<IReadOnlyList<ImportRow>> GetRowsAsync(int batchId, CancellationToken cancellationToken)
    {
        var rows = await _dataProvider.QueryAsync<ImportRow>(
            @"SELECT Id, BatchId, RowNumber, RawPayload, NormalizedOem, MatchedOemNumberId, ProposedProductId, ProposedFitmentJson, Confidence, ReviewStatus, ReviewNote
FROM TP_CE_ImportRow
WHERE BatchId = @batchId
ORDER BY RowNumber",
            new DataParameter("batchId", batchId));

        return rows.ToList();
    }

    public async Task ReplaceRowsAsync(int batchId, IReadOnlyList<ImportRow> rows, CancellationToken cancellationToken)
    {
        await _dataProvider.ExecuteNonQueryAsync(
            "DELETE FROM TP_CE_ImportRow WHERE BatchId = @batchId",
            new DataParameter("batchId", batchId));

        foreach (var row in rows)
        {
            await _dataProvider.ExecuteNonQueryAsync(
                @"INSERT INTO TP_CE_ImportRow
(BatchId, RowNumber, RawPayload, NormalizedOem, MatchedOemNumberId, ProposedProductId, ProposedFitmentJson, Confidence, ReviewStatus, ReviewNote)
VALUES
(@batchId, @rowNumber, @rawPayload, @normalizedOem, @matchedOemNumberId, @proposedProductId, @proposedFitmentJson, @confidence, @reviewStatus, @reviewNote)",
                new DataParameter("batchId", batchId),
                new DataParameter("rowNumber", row.RowNumber),
                new DataParameter("rawPayload", row.RawPayload),
                new DataParameter("normalizedOem", row.NormalizedOem),
                new DataParameter("matchedOemNumberId", row.MatchedOemNumberId),
                new DataParameter("proposedProductId", row.ProposedProductId),
                new DataParameter("proposedFitmentJson", row.ProposedFitmentJson),
                new DataParameter("confidence", row.Confidence),
                new DataParameter("reviewStatus", row.ReviewStatus),
                new DataParameter("reviewNote", row.ReviewNote));
        }
    }

    private static ImportBatch MapBatch(ImportBatchRow row)
        => new()
        {
            Id = row.Id,
            FileName = row.FileName,
            SourceFormat = (ImportSourceFormat)row.SourceFormatId,
            Status = row.Status,
            UploadedByCustomerId = row.UploadedByCustomerId,
            RowCount = row.RowCount,
            ErrorSummary = row.ErrorSummary,
            CreatedUtc = row.CreatedUtc,
            UpdatedUtc = row.UpdatedUtc
        };

    private sealed class ScalarIntRow
    {
        public int Value { get; set; }
    }

    private sealed class ImportBatchRow
    {
        public int Id { get; set; }
        public string FileName { get; set; } = string.Empty;
        public int SourceFormatId { get; set; }
        public string Status { get; set; } = string.Empty;
        public int? UploadedByCustomerId { get; set; }
        public int RowCount { get; set; }
        public string? ErrorSummary { get; set; }
        public System.DateTime CreatedUtc { get; set; }
        public System.DateTime UpdatedUtc { get; set; }
    }
}
