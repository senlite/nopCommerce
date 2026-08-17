using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace TwinParticles.CheckEngine.Domain.Fitment;

public interface IFitmentClaimReadRepository
{
    Task<IReadOnlyList<FitmentClaim>> GetClaimsAsync(int productId, int vehicleConfigurationId, CancellationToken cancellationToken);

    Task<IReadOnlyList<FitmentClaim>> GetReviewQueueAsync(CancellationToken cancellationToken);

    Task<IReadOnlyList<FitmentClaim>> GetClaimsByVendorIdAsync(int vendorId, CancellationToken cancellationToken)
        => Task.FromResult<IReadOnlyList<FitmentClaim>>([]);

    Task<IReadOnlyList<FitmentClaim>> GetAllClaimsAsync(CancellationToken cancellationToken)
        => Task.FromResult<IReadOnlyList<FitmentClaim>>([]);

    // Resolves a single claim by its primary key. Declared as a default member so existing
    // in-memory test doubles keep compiling; production repositories override it so review-time
    // events can locate the affected product/vehicle without changing every caller.
    Task<FitmentClaim?> GetByIdAsync(int claimId, CancellationToken cancellationToken)
        => Task.FromResult<FitmentClaim?>(null);
}
