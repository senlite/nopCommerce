using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using TwinParticles.CheckEngine.Domain.Ai;
using TwinParticles.CheckEngine.Domain.Search;

namespace TwinParticles.CheckEngine.Application.Ai;

public sealed class AiContentCandidateService
{
    private readonly IAiGenerationRepository? _generationRepository;

    public AiContentCandidateService(IAiGenerationRepository? generationRepository = null)
    {
        _generationRepository = generationRepository;
    }

    public async Task<int?> SaveCandidateAsync(
        AiGenerationEntityType entityType,
        int entityId,
        string featureKey,
        string locale,
        string outputText,
        string promptKey,
        string promptHash,
        decimal? qualityScore,
        CancellationToken cancellationToken)
    {
        if (_generationRepository is null || string.IsNullOrWhiteSpace(outputText))
            return null;

        var candidate = new AiGenerationCandidate
        {
            EntityType = entityType,
            EntityId = entityId,
            FeatureKey = featureKey,
            Locale = locale,
            OutputText = outputText,
            PromptKey = promptKey,
            PromptHash = promptHash,
            QualityScore = qualityScore,
            IsPublished = false,
            ReviewStatus = "pending",
            CreatedUtc = DateTimeOffset.UtcNow
        };

        return await _generationRepository.InsertAsync(candidate, cancellationToken);
    }

    public Task<IReadOnlyList<AiGenerationCandidate>> GetPendingAsync(
        AiGenerationEntityType entityType,
        int entityId,
        CancellationToken cancellationToken)
    {
        if (_generationRepository is null)
            return Task.FromResult<IReadOnlyList<AiGenerationCandidate>>([]);

        return _generationRepository.GetPendingByEntityAsync(entityType, entityId, cancellationToken);
    }

    public Task<IReadOnlyList<AiGenerationCandidate>> GetPendingQueueAsync(int take, CancellationToken cancellationToken)
    {
        if (_generationRepository is null)
            return Task.FromResult<IReadOnlyList<AiGenerationCandidate>>([]);

        return _generationRepository.GetPendingQueueAsync(take, cancellationToken);
    }

    public async Task<bool> ReviewAsync(int id, bool approved, string reviewer, CancellationToken cancellationToken)
    {
        if (_generationRepository is null || id <= 0)
            return false;

        var existing = await _generationRepository.GetByIdAsync(id, cancellationToken);
        if (existing is null)
            return false;

        await _generationRepository.MarkReviewedAsync(id, approved, reviewer, cancellationToken);
        return true;
    }
}
