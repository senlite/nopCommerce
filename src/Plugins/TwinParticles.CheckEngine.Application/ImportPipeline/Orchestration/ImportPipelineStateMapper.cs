using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using TwinParticles.CheckEngine.Domain.ImportPipeline;

namespace TwinParticles.CheckEngine.Application.ImportPipeline.Orchestration;

internal static class ImportPipelineStateMapper
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static ImportBatch ToEntity(ImportPipelineBatchState state, DateTime updatedUtc)
    {
        return new ImportBatch
        {
            Id = state.SqlBatchId ?? 0,
            CorrelationId = state.BatchId,
            FileName = state.FileName,
            SourceFormat = state.Format,
            Status = state.Status,
            RowCount = state.TotalRows,
            ErrorSummary = state.ErrorSummary,
            SourceContent = state.SourceContent,
            RunOptionsJson = JsonSerializer.Serialize(new RunOptions
            {
                DryRun = state.DryRun,
                SupplierProfileCode = state.SupplierProfileCode,
                EnableAiEnrichment = state.EnableAiEnrichment,
                EnableTranslation = state.EnableTranslation,
                EnableSeoGeneration = state.EnableSeoGeneration
            }, JsonOptions),
            CurrentStage = state.CurrentStage,
            CompletedStagesCsv = JoinStages(state.CompletedStages),
            CreatedUtc = state.CreatedUtc.UtcDateTime,
            UpdatedUtc = updatedUtc
        };
    }

    public static ImportPipelineBatchState FromEntities(ImportBatch batch, IReadOnlyList<ImportRow> rows)
    {
        var options = DeserializeOptions(batch.RunOptionsJson);
        return new ImportPipelineBatchState
        {
            BatchId = batch.CorrelationId ?? Guid.Empty,
            SqlBatchId = batch.Id,
            FileName = batch.FileName,
            Format = batch.SourceFormat,
            DryRun = options.DryRun,
            SourceContent = batch.SourceContent,
            SupplierProfileCode = options.SupplierProfileCode,
            EnableAiEnrichment = options.EnableAiEnrichment,
            EnableTranslation = options.EnableTranslation,
            EnableSeoGeneration = options.EnableSeoGeneration,
            Status = batch.Status,
            CurrentStage = batch.CurrentStage,
            CompletedStages = ParseStages(batch.CompletedStagesCsv),
            ErrorSummary = batch.ErrorSummary,
            CreatedUtc = new DateTimeOffset(DateTime.SpecifyKind(batch.CreatedUtc, DateTimeKind.Utc)),
            TotalRows = rows.Count,
            PublishedRows = rows.Count(row => DeserializeRow(row).IsPublished),
            FailedRows = rows.Count(row => !string.IsNullOrWhiteSpace(DeserializeRow(row).PublishError)),
            Rows = rows.Select(DeserializeRow).ToList()
        };
    }

    public static ImportRow ToEntity(int batchId, ImportPipelineRowState row)
    {
        row.CompletedStages ??= new HashSet<string>(StringComparer.Ordinal);
        return new ImportRow
        {
            BatchId = batchId,
            RowNumber = row.RowNumber,
            RawPayload = JsonSerializer.Serialize(row.Fields, JsonOptions),
            NormalizedOem = row.OemNumberNormalized,
            MatchedOemNumberId = row.OemNumberId,
            ProposedProductId = GetPublishedProductId(row),
            ProposedFitmentJson = row.VehicleConfigurationId is > 0
                ? JsonSerializer.Serialize(new
                {
                    row.VehicleConfigurationId,
                    row.VehicleMatchConfidence
                }, JsonOptions)
                : null,
            Confidence = row.IsDuplicate ? 0.4m : row.VehicleMatchConfidence,
            ReviewStatus = row.ReviewStatus,
            ReviewNote = row.ReviewReasonCode,
            PipelineStateJson = JsonSerializer.Serialize(row, JsonOptions),
            CompletedStagesCsv = JoinStages(row.CompletedStages),
            LastStageError = row.LastStageError
        };
    }

    private static ImportPipelineRowState DeserializeRow(ImportRow row)
    {
        ImportPipelineRowState? state = null;
        if (!string.IsNullOrWhiteSpace(row.PipelineStateJson))
            state = JsonSerializer.Deserialize<ImportPipelineRowState>(row.PipelineStateJson, JsonOptions);

        if (state is null)
        {
            var fields = JsonSerializer.Deserialize<Dictionary<string, string?>>(row.RawPayload, JsonOptions)
                         ?? new Dictionary<string, string?>();
            state = new ImportPipelineRowState
            {
                RowNumber = row.RowNumber,
                Fields = fields,
                OemNumberNormalized = row.NormalizedOem,
                OemNumberId = row.MatchedOemNumberId,
                ReviewStatus = row.ReviewStatus,
                ReviewReasonCode = row.ReviewNote
            };
        }

        state.CompletedStages = ParseStages(row.CompletedStagesCsv);
        state.LastStageError = row.LastStageError;
        return state;
    }

    private static RunOptions DeserializeOptions(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return new RunOptions();

        return JsonSerializer.Deserialize<RunOptions>(json, JsonOptions) ?? new RunOptions();
    }

    private static int? GetPublishedProductId(ImportPipelineRowState row)
    {
        if (row.Fields.TryGetValue("publishedProductId", out var value) &&
            int.TryParse(value, out var productId) &&
            productId > 0)
        {
            return productId;
        }

        return null;
    }

    private static string? JoinStages(IEnumerable<string> stages)
    {
        var values = stages.Where(value => !string.IsNullOrWhiteSpace(value))
            .Distinct(StringComparer.Ordinal)
            .OrderBy(value => value, StringComparer.Ordinal)
            .ToArray();
        return values.Length == 0 ? null : string.Join(',', values);
    }

    private static HashSet<string> ParseStages(string? csv)
    {
        return string.IsNullOrWhiteSpace(csv)
            ? new HashSet<string>(StringComparer.Ordinal)
            : csv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .ToHashSet(StringComparer.Ordinal);
    }

    private sealed class RunOptions
    {
        public bool DryRun { get; set; }
        public string? SupplierProfileCode { get; set; }
        public bool EnableAiEnrichment { get; set; }
        public bool EnableTranslation { get; set; }
        public bool EnableSeoGeneration { get; set; }
    }
}
