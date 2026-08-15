using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace TwinParticles.CheckEngine.Domain.Ai;

public interface IAiGenerationRepository
{
    Task<int> InsertAsync(AiGenerationCandidate candidate, CancellationToken cancellationToken);

    Task<AiGenerationCandidate?> GetByIdAsync(int id, CancellationToken cancellationToken);

    Task<IReadOnlyList<AiGenerationCandidate>> GetPendingByEntityAsync(
        AiGenerationEntityType entityType,
        int entityId,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<AiGenerationCandidate>> GetPendingQueueAsync(int take, CancellationToken cancellationToken)
        => Task.FromResult<IReadOnlyList<AiGenerationCandidate>>([]);

    Task MarkReviewedAsync(int id, bool approved, string reviewer, CancellationToken cancellationToken);
}
