using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TwinParticles.CheckEngine.Domain.Fitment;

namespace TwinParticles.CheckEngine.Application.Fitment;

public sealed class FitmentReviewService
{
    private readonly IFitmentClaimReadRepository _readRepository;
    private readonly IFitmentClaimWriteRepository _writeRepository;
    private readonly IFitmentReviewQueueRepository _reviewQueueRepository;

    public FitmentReviewService(
        IFitmentClaimReadRepository readRepository,
        IFitmentClaimWriteRepository writeRepository,
        IFitmentReviewQueueRepository reviewQueueRepository)
    {
        _readRepository = readRepository;
        _writeRepository = writeRepository;
        _reviewQueueRepository = reviewQueueRepository;
    }

    public Task<IReadOnlyList<FitmentClaim>> GetQueueAsync(CancellationToken cancellationToken)
    {
        return _readRepository.GetReviewQueueAsync(cancellationToken);
    }

    public async Task ApproveAsync(int claimId, CancellationToken cancellationToken)
    {
        await _writeRepository.SetStatusAsync(claimId, FitmentStatus.Fits, cancellationToken);
        await _writeRepository.SetPublishedAsync(claimId, true, cancellationToken);
    }

    public async Task RejectAsync(int claimId, CancellationToken cancellationToken)
    {
        await _writeRepository.SetStatusAsync(claimId, FitmentStatus.Rejected, cancellationToken);
        await _writeRepository.SetPublishedAsync(claimId, false, cancellationToken);
        await _reviewQueueRepository.EnqueueAsync(claimId, "fitment.rejected_by_reviewer", cancellationToken);
    }
}
