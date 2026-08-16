using System;
using System.Collections.Generic;
using System.Linq;
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
    private readonly IFitmentCache? _fitmentCache;

    public FitmentReviewService(
        IFitmentClaimReadRepository readRepository,
        IFitmentClaimWriteRepository writeRepository,
        IFitmentReviewQueueRepository reviewQueueRepository,
        ICheckEngineAuditService auditService,
        ISeoLandingRegenerationTrigger? seoLandingRegenerationTrigger = null,
        IFitmentCache? fitmentCache = null)
    {
        _readRepository = readRepository;
        _writeRepository = writeRepository;
        _reviewQueueRepository = reviewQueueRepository;
        _auditService = auditService;
        _seoLandingRegenerationTrigger = seoLandingRegenerationTrigger;
        _fitmentCache = fitmentCache;
    }

    public Task<IReadOnlyList<FitmentClaim>> GetQueueAsync(CancellationToken cancellationToken)
    {
        return _readRepository.GetReviewQueueAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<FitmentClaim>> GetAiQueueAsync(CancellationToken cancellationToken)
    {
        var queue = await _readRepository.GetReviewQueueAsync(cancellationToken);
        return queue.Where(claim => claim.SourceKindIsAi()).ToList();
    }

    public async Task ApproveAsync(int claimId, CancellationToken cancellationToken, string actor = "system")
    {
        if (claimId <= 0)
            throw new ArgumentException("Claim id must be positive.", nameof(claimId));

        var claim = await _readRepository.GetByIdAsync(claimId, cancellationToken);
        if (claim is not null && claim.SourceKindIsAi())
        {
            claim.Provenance.SourceKind = FitmentSourceKind.CuratorManual;
            claim.Provenance.SourceReference = "ai.review.promoted";
            claim.Provenance.CreatedBy = actor;
            await _writeRepository.UpsertAsync(claim, cancellationToken);
        }

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

        await AfterPublicationChangedAsync(claimId, cancellationToken);
    }

    public async Task RejectAsync(int claimId, CancellationToken cancellationToken, string actor = "system")
    {
        if (claimId <= 0)
            throw new ArgumentException("Claim id must be positive.", nameof(claimId));

        await _writeRepository.SetStatusAsync(claimId, FitmentStatus.Rejected, cancellationToken);
        await _writeRepository.SetPublishedAsync(claimId, false, cancellationToken);

        await _auditService.AppendAsync(
            actor,
            "fitment.reject",
            "FitmentClaim",
            claimId.ToString(),
            beforeJson: null,
            afterJson: "{\"status\":\"Rejected\",\"isPublished\":false}",
            cancellationToken);

        await AfterPublicationChangedAsync(claimId, cancellationToken);
    }

    // Refresh the fitment-gated landings and drop cached verdicts for the claim's product/vehicle
    // so indexability and storefront Fits flip in step with the review decision.
    private async Task AfterPublicationChangedAsync(int claimId, CancellationToken cancellationToken)
    {
        var claim = await _readRepository.GetByIdAsync(claimId, cancellationToken);
        if (claim is null)
            return;

        if (_fitmentCache is not null)
            await _fitmentCache.InvalidateAsync(claim.ProductId, claim.VehicleConfigurationId, cancellationToken);

        if (_seoLandingRegenerationTrigger is not null)
            await _seoLandingRegenerationTrigger.OnFitmentPublicationChangedAsync(claim.ProductId, claim.VehicleConfigurationId, cancellationToken);
    }
}
