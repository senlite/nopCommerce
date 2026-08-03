using System.Threading;
using System.Threading.Tasks;

namespace TwinParticles.CheckEngine.Domain.Fitment;

public interface IFitmentClaimWriteRepository
{
    Task UpsertAsync(FitmentClaim claim, CancellationToken cancellationToken);

    Task SetPublishedAsync(int claimId, bool isPublished, CancellationToken cancellationToken);

    Task SetStatusAsync(int claimId, FitmentStatus status, CancellationToken cancellationToken);
}
