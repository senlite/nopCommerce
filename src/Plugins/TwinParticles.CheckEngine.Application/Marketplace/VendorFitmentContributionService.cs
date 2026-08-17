using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TwinParticles.CheckEngine.Application.Licensing;
using TwinParticles.CheckEngine.Domain.Fitment;
using TwinParticles.CheckEngine.Domain.Marketplace;
using TwinParticles.CheckEngine.Domain.Security;

namespace TwinParticles.CheckEngine.Application.Marketplace;

/// <summary>
/// FR-856: vendor-attributed fitment proposals — list, submit, and revoke with isolation and audit.
/// </summary>
public sealed class VendorFitmentContributionService
{
    private readonly VendorIsolationService _isolation;
    private readonly IFitmentClaimReadRepository _fitmentClaims;
    private readonly IFitmentClaimWriteRepository _fitmentWrites;
    private readonly IFitmentReviewQueueRepository _fitmentQueue;
    private readonly MarketplaceLicenceGate _licenceGate;
    private readonly ICheckEngineAuditService _auditService;

    public VendorFitmentContributionService(
        VendorIsolationService isolation,
        IFitmentClaimReadRepository fitmentClaims,
        IFitmentClaimWriteRepository fitmentWrites,
        IFitmentReviewQueueRepository fitmentQueue,
        MarketplaceLicenceGate licenceGate,
        ICheckEngineAuditService auditService)
    {
        _isolation = isolation;
        _fitmentClaims = fitmentClaims;
        _fitmentWrites = fitmentWrites;
        _fitmentQueue = fitmentQueue;
        _licenceGate = licenceGate;
        _auditService = auditService;
    }

    public async Task<IReadOnlyList<FitmentClaim>> ListProposalsAsync(
        VendorActor actor,
        CancellationToken cancellationToken)
    {
        if (!await _licenceGate.AllowsMarketplaceAsync(cancellationToken) || actor.VendorId is not int vendorId)
            return [];

        var productIds = (await _isolation.ListCatalogAsync(actor, cancellationToken)).ToHashSet();
        var claims = await _fitmentClaims.GetClaimsByVendorIdAsync(vendorId, cancellationToken);

        return claims
            .Where(claim => productIds.Contains(claim.ProductId))
            .ToList();
    }

    public async Task<VendorDashboardMutationResult> SubmitProposalAsync(
        VendorActor actor,
        VendorFitmentProposalRequest request,
        CancellationToken cancellationToken)
    {
        if (!await _licenceGate.AllowsMarketplaceAsync(cancellationToken))
            return VendorDashboardMutationResult.Fail(VendorErrorCodes.LicenceDenied);

        if (actor.VendorId is not int vendorId)
            return VendorDashboardMutationResult.Fail(VendorErrorCodes.IsolationUnauthenticated);

        var decision = await _isolation.AuthorizeProductAsync(actor, request.ProductId, write: false, cancellationToken);
        if (!decision.Allowed)
            return VendorDashboardMutationResult.Fail(decision.ReasonCode ?? VendorErrorCodes.IsolationDenied);

        if (request.VehicleConfigurationId <= 0)
            return VendorDashboardMutationResult.Fail("vendor.fitment.invalid_vehicle");

        var claim = new FitmentClaim
        {
            ProductId = request.ProductId,
            VehicleConfigurationId = request.VehicleConfigurationId,
            VendorId = vendorId,
            Status = FitmentStatus.Unknown,
            Confidence = 0.5m,
            IsPublished = false,
            IsActive = true,
            Provenance = new FitmentClaimProvenance
            {
                SourceKind = FitmentSourceKind.SupplierCatalog,
                SourceReference = VendorSourceReference.ForVendor(vendorId),
                CreatedBy = $"vendor:{vendorId}",
                CreatedUtc = DateTimeOffset.UtcNow
            }
        };

        await _fitmentWrites.UpsertAsync(claim, cancellationToken);
        await _fitmentQueue.EnqueueAsync(claim.Id, "vendor.fitment.proposed", cancellationToken);

        await _auditService.AppendAsync(
            vendorId.ToString(),
            "vendor.fitment.proposed",
            "FitmentClaim",
            claim.Id.ToString(),
            beforeJson: null,
            afterJson: $"{{\"productId\":{request.ProductId},\"vehicleConfigurationId\":{request.VehicleConfigurationId},\"vendorId\":{vendorId}}}",
            cancellationToken);

        return VendorDashboardMutationResult.Ok(claim.Id);
    }

    public async Task<VendorDashboardMutationResult> RevokeProposalAsync(
        VendorActor actor,
        int claimId,
        CancellationToken cancellationToken)
    {
        if (!await _licenceGate.AllowsMarketplaceAsync(cancellationToken))
            return VendorDashboardMutationResult.Fail(VendorErrorCodes.LicenceDenied);

        if (claimId <= 0)
            return VendorDashboardMutationResult.Fail("vendor.fitment.invalid_claim");

        var claim = await _fitmentClaims.GetByIdAsync(claimId, cancellationToken);
        if (claim is null || !claim.IsActive)
            return VendorDashboardMutationResult.Fail("vendor.fitment.not_found");

        if (!VendorFitmentClaimRules.IsVendorContributed(claim))
            return VendorDashboardMutationResult.Fail("vendor.fitment.not_vendor_claim");

        if (!actor.CanBypassIsolation)
        {
            if (actor.VendorId is not int vendorId || !VendorFitmentClaimRules.OwnedByVendor(claim, vendorId))
                return VendorDashboardMutationResult.Fail(VendorErrorCodes.IsolationDenied);

            var decision = await _isolation.AuthorizeProductAsync(actor, claim.ProductId, write: false, cancellationToken);
            if (!decision.Allowed)
                return VendorDashboardMutationResult.Fail(decision.ReasonCode ?? VendorErrorCodes.IsolationDenied);
        }

        claim.IsActive = false;
        claim.IsPublished = false;
        claim.ValidToUtc = DateTime.UtcNow;
        await _fitmentWrites.UpsertAsync(claim, cancellationToken);
        await _fitmentWrites.SetPublishedAsync(claimId, false, cancellationToken);
        await _fitmentQueue.DequeueAsync(claimId, "vendor.fitment.revoked", cancellationToken);

        var actorLabel = actor.CanBypassIsolation
            ? "operator"
            : actor.VendorId?.ToString() ?? "anonymous";

        await _auditService.AppendAsync(
            actorLabel,
            "vendor.fitment.revoked",
            "FitmentClaim",
            claimId.ToString(),
            beforeJson: null,
            afterJson: $"{{\"vendorId\":{VendorFitmentClaimRules.ResolveVendorId(claim)},\"isActive\":false,\"isPublished\":false}}",
            cancellationToken);

        return VendorDashboardMutationResult.Ok(claimId);
    }
}
