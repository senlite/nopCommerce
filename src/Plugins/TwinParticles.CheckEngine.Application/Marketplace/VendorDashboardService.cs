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
/// FR-852 vendor dashboard aggregation; FR-860 scorecards. Inventory and fitment proposals are vendor-scoped.
/// </summary>
public sealed class VendorDashboardService
{
    public const int LowStockThreshold = 5;
    public const int OnTimeShipmentSlaDays = 3;

    private readonly VendorIsolationService _isolation;
    private readonly IVendorInventoryStore _inventory;
    private readonly IVendorAnalyticsStore _analytics;
    private readonly IVendorRepository _vendors;
    private readonly IFitmentClaimReadRepository _fitmentClaims;
    private readonly IFitmentClaimWriteRepository _fitmentWrites;
    private readonly IFitmentReviewQueueRepository _fitmentQueue;
    private readonly MarketplaceLicenceGate _licenceGate;
    private readonly ICheckEngineAuditService _auditService;

    public VendorDashboardService(
        VendorIsolationService isolation,
        IVendorInventoryStore inventory,
        IVendorAnalyticsStore analytics,
        IVendorRepository vendors,
        IFitmentClaimReadRepository fitmentClaims,
        IFitmentClaimWriteRepository fitmentWrites,
        IFitmentReviewQueueRepository fitmentQueue,
        MarketplaceLicenceGate licenceGate,
        ICheckEngineAuditService auditService)
    {
        _isolation = isolation;
        _inventory = inventory;
        _analytics = analytics;
        _vendors = vendors;
        _fitmentClaims = fitmentClaims;
        _fitmentWrites = fitmentWrites;
        _fitmentQueue = fitmentQueue;
        _licenceGate = licenceGate;
        _auditService = auditService;
    }

    public async Task<VendorDashboardSnapshot?> GetDashboardAsync(VendorActor actor, CancellationToken cancellationToken)
    {
        if (!await _licenceGate.AllowsMarketplaceAsync(cancellationToken))
            return null;

        if (actor.VendorId is null && !actor.CanBypassIsolation)
            return null;

        var productIds = await _isolation.ListCatalogAsync(actor, cancellationToken);
        var orders = await _isolation.ListOrdersAsync(actor, cancellationToken);
        var customers = await _isolation.ListCustomersAsync(actor, cancellationToken);
        var inventory = await _inventory.ListAsync(productIds, cancellationToken);

        var vendorId = actor.VendorId;
        string? vendorName = null;
        VendorScorecard scorecard;
        VendorFitmentProposalSummary fitmentSummary;

        if (vendorId is int id)
        {
            var vendor = await _vendors.GetByIdAsync(id, cancellationToken);
            vendorName = vendor?.TradingName ?? vendor?.LegalName;
            scorecard = await _analytics.GetScorecardAsync(id, vendorName, productIds, cancellationToken);
            fitmentSummary = await _analytics.GetFitmentProposalSummaryAsync(id, cancellationToken);
        }
        else
        {
            scorecard = await _analytics.GetScorecardAsync(0, "Marketplace", productIds, cancellationToken);
            fitmentSummary = new VendorFitmentProposalSummary();
        }

        return new VendorDashboardSnapshot
        {
            VendorId = vendorId,
            ProductCount = productIds.Count,
            TotalStockUnits = inventory.Sum(item => item.StockQuantity),
            LowStockCount = inventory.Count(item => item.StockQuantity <= LowStockThreshold),
            OrderCount = orders.Count,
            CustomerCount = customers.Count,
            FitmentProposals = fitmentSummary,
            Scorecard = scorecard,
            Statements = new VendorStatementPlaceholder
            {
                Available = false,
                MessageKey = "Plugins.TwinParticles.CheckEngine.Marketplace.Statements.Pending"
            }
        };
    }

    public async Task<IReadOnlyList<VendorInventoryItem>> ListInventoryAsync(
        VendorActor actor,
        CancellationToken cancellationToken)
    {
        if (!await _licenceGate.AllowsMarketplaceAsync(cancellationToken))
            return [];

        var productIds = await _isolation.ListCatalogAsync(actor, cancellationToken);
        return await _inventory.ListAsync(productIds, cancellationToken);
    }

    public async Task<VendorDashboardMutationResult> UpdateInventoryAsync(
        VendorActor actor,
        int productId,
        int stockQuantity,
        CancellationToken cancellationToken)
    {
        if (stockQuantity < 0)
            return VendorDashboardMutationResult.Fail("vendor.inventory.invalid_quantity");

        var decision = await _isolation.AuthorizeProductAsync(actor, productId, write: true, cancellationToken);
        if (!decision.Allowed)
            return VendorDashboardMutationResult.Fail(decision.ReasonCode ?? VendorErrorCodes.IsolationDenied);

        await _inventory.UpdateStockAsync(productId, stockQuantity, cancellationToken);

        await _auditService.AppendAsync(
            actor.VendorId?.ToString() ?? "operator",
            "vendor.inventory.updated",
            "Product",
            productId.ToString(),
            beforeJson: null,
            afterJson: $"{{\"stockQuantity\":{stockQuantity}}}",
            cancellationToken);

        return VendorDashboardMutationResult.Ok();
    }

    public async Task<VendorScorecard?> GetScorecardAsync(VendorActor actor, CancellationToken cancellationToken)
    {
        if (!await _licenceGate.AllowsMarketplaceAsync(cancellationToken))
            return null;

        if (actor.VendorId is int vendorId)
        {
            var vendor = await _vendors.GetByIdAsync(vendorId, cancellationToken);
            var productIds = await _isolation.ListCatalogAsync(actor, cancellationToken);
            return await _analytics.GetScorecardAsync(
                vendorId,
                vendor?.TradingName ?? vendor?.LegalName,
                productIds,
                cancellationToken);
        }

        if (!actor.CanBypassIsolation)
            return null;

        var allProducts = await _isolation.ListCatalogAsync(actor, cancellationToken);
        return await _analytics.GetScorecardAsync(0, "Marketplace", allProducts, cancellationToken);
    }

    public async Task<IReadOnlyList<VendorScorecard>> ListOperatorScorecardsAsync(
        VendorActor actor,
        CancellationToken cancellationToken)
    {
        if (!await _licenceGate.AllowsMarketplaceAsync(cancellationToken) || !actor.CanBypassIsolation)
            return [];

        var vendors = await _vendors.GetByStatusesAsync([VendorStatus.Active, VendorStatus.Suspended], cancellationToken);
        var results = new List<VendorScorecard>();

        foreach (var vendor in vendors.Where(v => !v.IsOperator))
        {
            var productIds = await _isolation.ListCatalogAsync(VendorActor.Vendor(vendor.Id), cancellationToken);
            results.Add(await _analytics.GetScorecardAsync(
                vendor.Id,
                vendor.TradingName ?? vendor.LegalName,
                productIds,
                cancellationToken));
        }

        return results;
    }

    public async Task<IReadOnlyList<FitmentClaim>> ListFitmentProposalsAsync(
        VendorActor actor,
        CancellationToken cancellationToken)
    {
        if (!await _licenceGate.AllowsMarketplaceAsync(cancellationToken) || actor.VendorId is not int vendorId)
            return [];

        var productIds = (await _isolation.ListCatalogAsync(actor, cancellationToken)).ToHashSet();
        var prefix = VendorSourceReference.ForVendor(vendorId);
        var claims = await _fitmentClaims.GetAllClaimsAsync(cancellationToken);

        return claims
            .Where(claim => claim.Provenance.SourceReference.StartsWith(prefix, StringComparison.Ordinal)
                && productIds.Contains(claim.ProductId))
            .OrderByDescending(claim => claim.Provenance.CreatedUtc)
            .ToList();
    }

    public async Task<VendorDashboardMutationResult> SubmitFitmentProposalAsync(
        VendorActor actor,
        VendorFitmentProposalRequest request,
        CancellationToken cancellationToken)
    {
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
            afterJson: $"{{\"productId\":{request.ProductId},\"vehicleConfigurationId\":{request.VehicleConfigurationId}}}",
            cancellationToken);

        return VendorDashboardMutationResult.Ok(claim.Id);
    }
}

public sealed class VendorFitmentProposalRequest
{
    public int ProductId { get; init; }

    public int VehicleConfigurationId { get; init; }
}

public sealed class VendorDashboardMutationResult
{
    public bool Succeeded { get; init; }

    public string? ReasonCode { get; init; }

    public int? ClaimId { get; init; }

    public static VendorDashboardMutationResult Ok(int? claimId = null)
        => new() { Succeeded = true, ClaimId = claimId };

    public static VendorDashboardMutationResult Fail(string reasonCode)
        => new() { Succeeded = false, ReasonCode = reasonCode };
}
