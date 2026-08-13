using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TwinParticles.CheckEngine.Domain.Fitment;
using TwinParticles.CheckEngine.Domain.Security;
using TwinParticles.CheckEngine.Domain.Seo;

namespace TwinParticles.CheckEngine.Application.Fitment;

public sealed class FitmentReviewService
{
    private readonly IFitmentClaimReadRepository _readRepository;
    private readonly IFitmentClaimWriteRepository _writeRepository;
    private readonly IFitmentReviewQueueRepository _reviewQueueRepository;
    private readonly ICheckEngineAuditService _auditService;
    private readonly ISeoLandingRegenerationTrigger? _seoLandingRegenerationTrigger;

    public FitmentReviewService(
        IFitmentClaimReadRepository readRepository,
        IFitmentClaimWriteRepository writeRepository,
        IFitmentReviewQueueRepository reviewQueueRepository,
        ICheckEngineAuditService auditService,
        ISeoLandingRegenerationTrigger? seoLandingRegenerationTrigger = null)
    {
        _readRepository = readRepository;
        _writeRepository = writeRepository;
        _reviewQueueRepository = reviewQueueRepository;
        _auditService = auditService;
        _seoLandingRegenerationTrigger = seoLandingRegenerationTrigger;
    }

    public Task<IReadOnlyList<FitmentClaim>> GetQueueAsync(CancellationToken cancellationToken)
    {
        return _readRepository.GetReviewQueueAsync(cancellationToken);
    }

    public async Task ApproveAsync(int claimId, CancellationToken cancellationToken, string actor = "system")
    {
        if (claimId <= 0)
            throw new ArgumentException("Claim id must be positive.", nameof(claimId));

        await _writeRepository.SetStatusAsync(claimId, FitmentStatus.Fits, cancellationToken);
        await _writeRepository.SetPublishedAsync(claimId, true, cancellationToken);

        await _auditService.AppendAsync(
            actor,
            "fitment.approve",
            "FitmentClaim",
            claimId.ToString(),
            beforeJson: null,
            afterJson: "{\"status\":\"Fits\",\"isPublished\":true}",
            cancellationToken);

        await RegenerateLandingsAsync(claimId, cancellationToken);
    }

    public async Task RejectAsync(int claimId, CancellationToken cancellationToken, string actor = "system")
    {
        if (claimId <= 0)
            throw new ArgumentException("Claim id must be positive.", nameof(claimId));

        await _writeRepository.SetStatusAsync(claimId, FitmentStatus.Rejected, cancellationToken);
        await _writeRepository.SetPublishedAsync(claimId, false, cancellationToken);
        await _reviewQueueRepository.EnqueueAsync(claimId, "fitment.rejected_by_reviewer", cancellationToken);

        await _auditService.AppendAsync(
            actor,
            "fitment.reject",
            "FitmentClaim",
            claimId.ToString(),
            beforeJson: null,
            afterJson: "{\"status\":\"Rejected\",\"isPublished\":false}",
            cancellationToken);

        await RegenerateLandingsAsync(claimId, cancellationToken);
    }

    // Refresh the fitment-gated landings for the claim's product/vehicle so indexability flips
    // in step with the review decision. The claim lookup is best-effort: if it cannot be resolved
    // (e.g. a legacy repository without id lookup), regeneration is skipped rather than failing
    // the review action.
    private async Task RegenerateLandingsAsync(int claimId, CancellationToken cancellationToken)
    {
        if (_seoLandingRegenerationTrigger is null)
            return;

        var claim = await _readRepository.GetByIdAsync(claimId, cancellationToken);
        if (claim is null)
            return;

        await _seoLandingRegenerationTrigger.OnFitmentPublicationChangedAsync(claim.ProductId, claim.VehicleConfigurationId, cancellationToken);
    }
}
