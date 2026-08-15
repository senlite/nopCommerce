using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LinqToDB.Data;
using Nop.Data;
using TwinParticles.CheckEngine.Domain.Ai;

namespace TwinParticles.CheckEngine.Infrastructure.Ai;

public sealed class SqlAiGenerationRepository : IAiGenerationRepository
{
    private readonly INopDataProvider _dataProvider;

    public SqlAiGenerationRepository(INopDataProvider dataProvider)
    {
        _dataProvider = dataProvider;
    }

    public async Task<int> InsertAsync(AiGenerationCandidate candidate, CancellationToken cancellationToken)
    {
        var id = await _dataProvider.QueryAsync<int>(@"
INSERT INTO TP_CE_AiGeneration
(EntityTypeId, EntityId, FeatureKey, Locale, OutputText, PromptKey, PromptHash, QualityScore, IsPublished, ReviewStatus, Reviewer, CreatedUtc, ReviewedUtc)
VALUES
(@entityTypeId, @entityId, @featureKey, @locale, @outputText, @promptKey, @promptHash, @qualityScore, @isPublished, @reviewStatus, @reviewer, @createdUtc, @reviewedUtc);
SELECT CAST(SCOPE_IDENTITY() AS INT);",
            new DataParameter("entityTypeId", (int)candidate.EntityType),
            new DataParameter("entityId", candidate.EntityId),
            new DataParameter("featureKey", candidate.FeatureKey),
            new DataParameter("locale", candidate.Locale),
            new DataParameter("outputText", candidate.OutputText),
            new DataParameter("promptKey", candidate.PromptKey),
            new DataParameter("promptHash", candidate.PromptHash),
            new DataParameter("qualityScore", candidate.QualityScore),
            new DataParameter("isPublished", candidate.IsPublished),
            new DataParameter("reviewStatus", candidate.ReviewStatus),
            new DataParameter("reviewer", candidate.Reviewer ?? (object)DBNull.Value),
            new DataParameter("createdUtc", candidate.CreatedUtc.UtcDateTime),
            new DataParameter("reviewedUtc", candidate.ReviewedUtc?.UtcDateTime ?? (object)DBNull.Value));

        return id.FirstOrDefault();
    }

    public async Task<AiGenerationCandidate?> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        var rows = await _dataProvider.QueryAsync<AiGenerationRow>(@"
SELECT Id, EntityTypeId, EntityId, FeatureKey, Locale, OutputText, PromptKey, PromptHash, QualityScore, IsPublished, ReviewStatus, Reviewer, CreatedUtc, ReviewedUtc
FROM TP_CE_AiGeneration
WHERE Id = @id",
            new DataParameter("id", id));

        return rows.Select(Map).FirstOrDefault();
    }

    public async Task<IReadOnlyList<AiGenerationCandidate>> GetPendingByEntityAsync(
        AiGenerationEntityType entityType,
        int entityId,
        CancellationToken cancellationToken)
    {
        var rows = await _dataProvider.QueryAsync<AiGenerationRow>(@"
SELECT Id, EntityTypeId, EntityId, FeatureKey, Locale, OutputText, PromptKey, PromptHash, QualityScore, IsPublished, ReviewStatus, Reviewer, CreatedUtc, ReviewedUtc
FROM TP_CE_AiGeneration
WHERE EntityTypeId = @entityTypeId AND EntityId = @entityId AND ReviewStatus = 'pending'
ORDER BY CreatedUtc DESC",
            new DataParameter("entityTypeId", (int)entityType),
            new DataParameter("entityId", entityId));

        return rows.Select(Map).ToList();
    }

    public async Task<IReadOnlyList<AiGenerationCandidate>> GetPendingQueueAsync(int take, CancellationToken cancellationToken)
    {
        var rows = await _dataProvider.QueryAsync<AiGenerationRow>(@"
SELECT Id, EntityTypeId, EntityId, FeatureKey, Locale, OutputText, PromptKey, PromptHash, QualityScore, IsPublished, ReviewStatus, Reviewer, CreatedUtc, ReviewedUtc
FROM TP_CE_AiGeneration
WHERE ReviewStatus = 'pending'
ORDER BY CreatedUtc DESC");

        return rows.Select(Map).Take(Math.Max(1, take)).ToList();
    }

    public async Task MarkReviewedAsync(int id, bool approved, string reviewer, CancellationToken cancellationToken)
    {
        await _dataProvider.ExecuteNonQueryAsync(@"
UPDATE TP_CE_AiGeneration
SET ReviewStatus = @reviewStatus,
    Reviewer = @reviewer,
    ReviewedUtc = @reviewedUtc,
    IsPublished = @isPublished
WHERE Id = @id",
            new DataParameter("id", id),
            new DataParameter("reviewStatus", approved ? "approved" : "rejected"),
            new DataParameter("reviewer", reviewer),
            new DataParameter("reviewedUtc", DateTime.UtcNow),
            new DataParameter("isPublished", approved));
    }

    private static AiGenerationCandidate Map(AiGenerationRow row)
    {
        return new AiGenerationCandidate
        {
            Id = row.Id,
            EntityType = (AiGenerationEntityType)row.EntityTypeId,
            EntityId = row.EntityId,
            FeatureKey = row.FeatureKey,
            Locale = row.Locale,
            OutputText = row.OutputText,
            PromptKey = row.PromptKey,
            PromptHash = row.PromptHash,
            QualityScore = row.QualityScore,
            IsPublished = row.IsPublished,
            ReviewStatus = row.ReviewStatus,
            Reviewer = row.Reviewer,
            CreatedUtc = new DateTimeOffset(DateTime.SpecifyKind(row.CreatedUtc, DateTimeKind.Utc)),
            ReviewedUtc = row.ReviewedUtc.HasValue
                ? new DateTimeOffset(DateTime.SpecifyKind(row.ReviewedUtc.Value, DateTimeKind.Utc))
                : null
        };
    }

    private sealed class AiGenerationRow
    {
        public int Id { get; set; }
        public int EntityTypeId { get; set; }
        public int EntityId { get; set; }
        public string FeatureKey { get; set; } = string.Empty;
        public string Locale { get; set; } = string.Empty;
        public string OutputText { get; set; } = string.Empty;
        public string PromptKey { get; set; } = string.Empty;
        public string PromptHash { get; set; } = string.Empty;
        public decimal? QualityScore { get; set; }
        public bool IsPublished { get; set; }
        public string ReviewStatus { get; set; } = string.Empty;
        public string? Reviewer { get; set; }
        public DateTime CreatedUtc { get; set; }
        public DateTime? ReviewedUtc { get; set; }
    }
}
