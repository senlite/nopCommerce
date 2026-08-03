using System.Threading;
using System.Threading.Tasks;
using TwinParticles.CheckEngine.Domain.Fitment;

namespace TwinParticles.CheckEngine.Application.Fitment;

public sealed class FitmentPublicationPolicyService
{
    private readonly IFitmentClaimWriteRepository _writeRepository;

    public FitmentPublicationPolicyService(IFitmentClaimWriteRepository writeRepository)
    {
        _writeRepository = writeRepository;
    }

    public async Task<bool> TryPublishAsync(FitmentClaim claim, CancellationToken cancellationToken)
    {
        if (claim.SourceKindIsAi() || claim.Confidence < 0.85m)
            return false;

        if (claim.SafetyClass == SafetyClass.SafetyCritical && claim.Confidence < 0.95m)
            return false;

        claim.IsPublished = true;
        await _writeRepository.UpsertAsync(claim, cancellationToken);
        await _writeRepository.SetPublishedAsync(claim.Id, true, cancellationToken);
        return true;
    }
}
