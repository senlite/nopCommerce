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
    private readonly VendorFitmentContributionService _fitmentContributions;
    private readonly PayoutStatementService _payoutService;
    private readonly MarketplaceLicenceGate _licenceGate;
    private readonly ICheckEngineAuditService _auditService;

    public VendorDashboardService(
        VendorIsolationService isolation,
        IVendorInventoryStore inventory,
        IVendorAnalyticsStore analytics,
        IVendorRepository vendors,
        VendorFitmentContributionService fitmentContributions,
        PayoutStatementService payoutService,
        MarketplaceLicenceGate licenceGate,
        ICheckEngineAuditService auditService)
    {
        _isolation = isolation;
        _inventory = inventory;
        _analytics = analytics;
        _vendors = vendors;
        _fitmentContributions = fitmentContributions;
        _payoutService = payoutService;
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

        var statementSummary = await BuildStatementSummaryAsync(vendorId, cancellationToken);

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
            Statements = statementSummary
        };
    }

    public async Task<IReadOnlyList<PayoutStatement>> ListStatementsAsync(
        VendorActor actor,
        CancellationToken cancellationToken)
    {
        if (!await _licenceGate.AllowsMarketplaceAsync(cancellationToken) || actor.VendorId is not int vendorId)
            return [];

        return await _payoutService.ListAsync(vendorId, cancellationToken);
    }

    private async Task<VendorStatementSummary> BuildStatementSummaryAsync(int? vendorId, CancellationToken cancellationToken)
    {
        if (vendorId is not int id)
            return new VendorStatementSummary { Available = false, MessageKey = "Plugins.TwinParticles.CheckEngine.Marketplace.Statements.Pending" };

        var latest = await _payoutService.GetLatestForVendorAsync(id, cancellationToken);
        if (latest is null)
            return new VendorStatementSummary { Available = false, MessageKey = "Plugins.TwinParticles.CheckEngine.Marketplace.Statements.None" };

        return new VendorStatementSummary
        {
            Available = true,
            StatementId = latest.Id,
            NetPayout = latest.NetPayout,
            Status = latest.Status,
            PeriodStartUtc = latest.PeriodStartUtc,
            PeriodEndUtc = latest.PeriodEndUtc
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

    public Task<IReadOnlyList<FitmentClaim>> ListFitmentProposalsAsync(
        VendorActor actor,
        CancellationToken cancellationToken)
        => _fitmentContributions.ListProposalsAsync(actor, cancellationToken);

    public Task<VendorDashboardMutationResult> SubmitFitmentProposalAsync(
        VendorActor actor,
        VendorFitmentProposalRequest request,
        CancellationToken cancellationToken)
        => _fitmentContributions.SubmitProposalAsync(actor, request, cancellationToken);

    public Task<VendorDashboardMutationResult> RevokeFitmentProposalAsync(
        VendorActor actor,
        int claimId,
        CancellationToken cancellationToken)
        => _fitmentContributions.RevokeProposalAsync(actor, claimId, cancellationToken);
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
