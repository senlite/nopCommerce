using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace TwinParticles.CheckEngine.Domain.Marketplace;

public interface IVendorAnalyticsStore
{
    Task<VendorScorecard> GetScorecardAsync(
        int vendorId,
        string? vendorName,
        IReadOnlyCollection<int> productIds,
        CancellationToken cancellationToken);

    Task<VendorFitmentProposalSummary> GetFitmentProposalSummaryAsync(
        int vendorId,
        CancellationToken cancellationToken);
}
