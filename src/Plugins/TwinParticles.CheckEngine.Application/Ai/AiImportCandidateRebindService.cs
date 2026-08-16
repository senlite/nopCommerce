using System.Threading;
using System.Threading.Tasks;
using TwinParticles.CheckEngine.Domain.Ai;

namespace TwinParticles.CheckEngine.Application.Ai;

/// <summary>
/// Rebinds import-stage AI candidates from import row numbers to published nopCommerce product ids.
/// </summary>
public sealed class AiImportCandidateRebindService
{
    private readonly IAiGenerationRepository? _generationRepository;

    public AiImportCandidateRebindService(IAiGenerationRepository? generationRepository = null)
    {
        _generationRepository = generationRepository;
    }

    public Task RebindImportRowAsync(int importRowNumber, int productId, CancellationToken cancellationToken)
    {
        if (_generationRepository is null || importRowNumber <= 0 || productId <= 0)
            return Task.CompletedTask;

        return _generationRepository.RebindImportRowEntityAsync(importRowNumber, productId, cancellationToken);
    }
}
