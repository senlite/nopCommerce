using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace TwinParticles.CheckEngine.Domain.Fitment;

public interface IFitmentClaimReadRepository
{
    Task<IReadOnlyList<FitmentClaim>> GetClaimsAsync(int productId, int vehicleConfigurationId, CancellationToken cancellationToken);

    Task<IReadOnlyList<FitmentClaim>> GetReviewQueueAsync(CancellationToken cancellationToken);
}
