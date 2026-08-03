using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TwinParticles.CheckEngine.Domain.Fitment;

namespace TwinParticles.CheckEngine.Infrastructure.Fitment;

public sealed class InMemoryFitmentClaimRepository : IFitmentClaimReadRepository, IFitmentClaimWriteRepository
{
    private readonly List<FitmentClaim> _claims = [];

    public Task<IReadOnlyList<FitmentClaim>> GetClaimsAsync(int productId, int vehicleConfigurationId, CancellationToken cancellationToken)
    {
        var results = _claims.Where(x => x.ProductId == productId && x.VehicleConfigurationId == vehicleConfigurationId).ToList();
        return Task.FromResult<IReadOnlyList<FitmentClaim>>(results);
    }

    public Task<IReadOnlyList<FitmentClaim>> GetReviewQueueAsync(CancellationToken cancellationToken)
    {
        var queue = _claims
            .Where(x => !x.IsPublished || x.Status == FitmentStatus.Unknown || x.Status == FitmentStatus.Rejected)
            .OrderByDescending(x => x.Confidence)
            .ToList();

        return Task.FromResult<IReadOnlyList<FitmentClaim>>(queue);
    }

    public Task UpsertAsync(FitmentClaim claim, CancellationToken cancellationToken)
    {
        var existing = _claims.FirstOrDefault(x => x.Id == claim.Id);
        if (existing is null)
        {
            if (claim.Id == 0)
                claim.Id = _claims.Count + 1;

            _claims.Add(claim);
        }
        else
        {
            _claims.Remove(existing);
            _claims.Add(claim);
        }

        return Task.CompletedTask;
    }

    public Task SetPublishedAsync(int claimId, bool isPublished, CancellationToken cancellationToken)
    {
        var claim = _claims.FirstOrDefault(x => x.Id == claimId);
        if (claim is not null)
            claim.IsPublished = isPublished;

        return Task.CompletedTask;
    }

    public Task SetStatusAsync(int claimId, FitmentStatus status, CancellationToken cancellationToken)
    {
        var claim = _claims.FirstOrDefault(x => x.Id == claimId);
        if (claim is not null)
            claim.Status = status;

        return Task.CompletedTask;
    }
}
