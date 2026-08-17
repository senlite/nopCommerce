using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Nop.Core;
using Nop.Services.Customers;
using Nop.Services.Security;
using Nop.Web.Controllers;
using Nop.Web.Framework.Mvc.Filters;
using TwinParticles.CheckEngine.Application.Licensing;
using TwinParticles.CheckEngine.Application.Marketplace;
using TwinParticles.CheckEngine.Domain.Marketplace;
using TwinParticles.CheckEngine.Models;
using TwinParticles.CheckEngine.Security;

namespace TwinParticles.CheckEngine.Controllers;

[AutoValidateAntiforgeryToken]
public sealed class VendorController : BasePublicController
{
    private readonly VendorOnboardingService _onboardingService;
    private readonly MarketplaceLicenceGate _marketplaceGate;
    private readonly IMarketplaceOnboardingPolicy _policy;
    private readonly VendorIsolationService _isolationService;
    private readonly VendorDashboardService _dashboardService;
    private readonly IVendorRepository _vendors;
    private readonly IWorkContext _workContext;
    private readonly ICustomerService _customerService;
    private readonly IPermissionService _permissionService;

    public VendorController(
        VendorOnboardingService onboardingService,
        MarketplaceLicenceGate marketplaceGate,
        IMarketplaceOnboardingPolicy policy,
        VendorIsolationService isolationService,
        VendorDashboardService dashboardService,
        IVendorRepository vendors,
        IWorkContext workContext,
        ICustomerService customerService,
        IPermissionService permissionService)
    {
        _onboardingService = onboardingService;
        _marketplaceGate = marketplaceGate;
        _policy = policy;
        _isolationService = isolationService;
        _dashboardService = dashboardService;
        _vendors = vendors;
        _workContext = workContext;
        _customerService = customerService;
        _permissionService = permissionService;
    }

    [HttpGet]
    public async Task<IActionResult> Apply(CancellationToken cancellationToken)
    {
        if (!await _marketplaceGate.AllowsMarketplaceAsync(cancellationToken) || !_policy.ApplicationsOpen)
            return NotFound();

        return View("~/Plugins/TwinParticles.CheckEngine/Views/Vendor/Apply.cshtml");
    }

    [HttpPost]
    public async Task<IActionResult> Apply([FromBody] VendorApplyRequestModel model, CancellationToken cancellationToken)
    {
        if (!await _marketplaceGate.AllowsMarketplaceAsync(cancellationToken))
            return Denied(VendorErrorCodes.LicenceDenied, 403);

        var customer = await _workContext.GetCurrentCustomerAsync();
        var applicantId = await _customerService.IsGuestAsync(customer) ? (int?)null : customer.Id;

        var result = await _onboardingService.ApplyAsync(new VendorApplication
        {
            LegalName = model.LegalName,
            TradingName = model.TradingName,
            ContactEmail = model.ContactEmail,
            ApplicantCustomerId = applicantId,
            TaxIdsJson = model.TaxIdsJson,
            CategoriesCsv = model.CategoriesCsv,
            BankingDetails = model.BankingDetails
        }, cancellationToken);

        if (!result.Succeeded || result.Snapshot is null)
            return Denied(result.ReasonCode ?? VendorErrorCodes.InvalidApplication, 400);

        var vendorId = result.Snapshot.Vendor.Id;
        if (model.AcceptAgreement)
            result = await _onboardingService.AcceptAgreementAsync(vendorId, model.ContactEmail, cancellationToken);

        if (result.Succeeded && model.SubmitForReview)
            result = await _onboardingService.SubmitForReviewAsync(vendorId, model.ContactEmail, cancellationToken);

        return result.Succeeded
            ? Json(result.Snapshot)
            : Denied(result.ReasonCode ?? VendorErrorCodes.IllegalTransition, 400);
    }

    [HttpPost]
    public async Task<IActionResult> AcceptAgreement(int vendorId, CancellationToken cancellationToken)
    {
        if (!await _marketplaceGate.AllowsMarketplaceAsync(cancellationToken))
            return Denied(VendorErrorCodes.LicenceDenied, 403);

        var customer = await _workContext.GetCurrentCustomerAsync();
        var result = await _onboardingService.AcceptAgreementAsync(vendorId, customer.Email, cancellationToken);
        return result.Succeeded
            ? Json(result.Snapshot)
            : Denied(result.ReasonCode ?? VendorErrorCodes.IllegalTransition, 400);
    }

    [HttpGet]
    public async Task<IActionResult> Status(int vendorId, CancellationToken cancellationToken)
    {
        if (!await _marketplaceGate.AllowsMarketplaceAsync(cancellationToken))
            return Denied(VendorErrorCodes.LicenceDenied, 403);

        var snapshot = await _onboardingService.GetSnapshotAsync(vendorId, cancellationToken);
        return snapshot is null ? NotFound() : Json(snapshot);
    }

    [HttpGet]
    public async Task<IActionResult> Dashboard(CancellationToken cancellationToken)
    {
        if (!await _marketplaceGate.AllowsMarketplaceAsync(cancellationToken))
            return NotFound();

        var actor = await ResolveActorAsync(cancellationToken);
        if (actor.VendorId is null && !actor.CanBypassIsolation)
            return NotFound();

        return View("~/Plugins/TwinParticles.CheckEngine/Views/Vendor/Dashboard.cshtml");
    }

    [HttpGet]
    public async Task<IActionResult> DashboardData(CancellationToken cancellationToken)
    {
        var snapshot = await _dashboardService.GetDashboardAsync(await ResolveActorAsync(cancellationToken), cancellationToken);
        return snapshot is null ? Denied(VendorErrorCodes.IsolationUnauthenticated, 403) : Json(snapshot);
    }

    [HttpGet]
    public async Task<IActionResult> Inventory(CancellationToken cancellationToken)
        => Json(await _dashboardService.ListInventoryAsync(await ResolveActorAsync(cancellationToken), cancellationToken));

    [HttpPost]
    public async Task<IActionResult> UpdateInventory([FromBody] VendorInventoryUpdateModel model, CancellationToken cancellationToken)
    {
        var result = await _dashboardService.UpdateInventoryAsync(
            await ResolveActorAsync(cancellationToken), model.ProductId, model.StockQuantity, cancellationToken);
        return result.Succeeded
            ? Json(new { model.ProductId, model.StockQuantity })
            : Denied(result.ReasonCode ?? VendorErrorCodes.IsolationDenied, 403);
    }

    [HttpGet]
    public async Task<IActionResult> Scorecard(CancellationToken cancellationToken)
    {
        var scorecard = await _dashboardService.GetScorecardAsync(await ResolveActorAsync(cancellationToken), cancellationToken);
        return scorecard is null ? Denied(VendorErrorCodes.IsolationUnauthenticated, 403) : Json(scorecard);
    }

    [HttpGet]
    public async Task<IActionResult> FitmentProposals(CancellationToken cancellationToken)
        => Json(await _dashboardService.ListFitmentProposalsAsync(await ResolveActorAsync(cancellationToken), cancellationToken));

    [HttpPost]
    public async Task<IActionResult> SubmitFitmentProposal([FromBody] VendorFitmentProposalModel model, CancellationToken cancellationToken)
    {
        var result = await _dashboardService.SubmitFitmentProposalAsync(
            await ResolveActorAsync(cancellationToken),
            new VendorFitmentProposalRequest { ProductId = model.ProductId, VehicleConfigurationId = model.VehicleConfigurationId },
            cancellationToken);
        return result.Succeeded
            ? Json(new { claimId = result.ClaimId })
            : Denied(result.ReasonCode ?? VendorErrorCodes.IsolationDenied, 400);
    }

    [HttpGet]
    public async Task<IActionResult> Catalog(CancellationToken cancellationToken)
        => Json(await _isolationService.ListCatalogAsync(await ResolveActorAsync(cancellationToken), cancellationToken));

    [HttpGet]
    public async Task<IActionResult> Product(int productId, CancellationToken cancellationToken)
    {
        var decision = await _isolationService.AuthorizeProductAsync(
            await ResolveActorAsync(cancellationToken), productId, write: false, cancellationToken);
        return decision.Allowed ? Json(new { productId }) : Denied(decision.ReasonCode ?? VendorErrorCodes.IsolationDenied, 403);
    }

    [HttpPost]
    public async Task<IActionResult> EditProduct([FromBody] VendorProductEditModel model, CancellationToken cancellationToken)
    {
        var decision = await _isolationService.AuthorizeProductAsync(
            await ResolveActorAsync(cancellationToken), model.ProductId, write: true, cancellationToken);
        return decision.Allowed
            ? Json(new { model.ProductId })
            : Denied(decision.ReasonCode ?? VendorErrorCodes.IsolationDenied, 403);
    }

    [HttpGet]
    public async Task<IActionResult> Orders(CancellationToken cancellationToken)
        => Json(await _isolationService.ListOrdersAsync(await ResolveActorAsync(cancellationToken), cancellationToken));

    [HttpGet]
    public async Task<IActionResult> Order(int orderId, CancellationToken cancellationToken)
    {
        var decision = await _isolationService.AuthorizeOrderAsync(
            await ResolveActorAsync(cancellationToken), orderId, cancellationToken);
        return decision.Allowed ? Json(new { orderId }) : Denied(decision.ReasonCode ?? VendorErrorCodes.IsolationDenied, 403);
    }

    [HttpGet]
    public async Task<IActionResult> Customers(CancellationToken cancellationToken)
        => Json(await _isolationService.ListCustomersAsync(await ResolveActorAsync(cancellationToken), cancellationToken));

    [HttpGet]
    public async Task<IActionResult> Customer(int customerId, CancellationToken cancellationToken)
    {
        var decision = await _isolationService.AuthorizeCustomerAsync(
            await ResolveActorAsync(cancellationToken), customerId, cancellationToken);
        return decision.Allowed ? Json(new { customerId }) : Denied(decision.ReasonCode ?? VendorErrorCodes.IsolationDenied, 403);
    }

    private async Task<VendorActor> ResolveActorAsync(CancellationToken cancellationToken)
    {
        if (await _permissionService.AuthorizeAsync(CheckEnginePermissionProvider.ManageCheckEngine.SystemName))
            return VendorActor.OperatorAdmin;

        var customer = await _workContext.GetCurrentCustomerAsync();
        if (await _customerService.IsGuestAsync(customer))
            return VendorActor.Anonymous;

        var vendor = await _vendors.GetByApplicantCustomerIdAsync(customer.Id, cancellationToken);
        if (vendor is { Status: VendorStatus.Active } || vendor is { IsOperator: true })
            return VendorActor.Vendor(vendor.Id);

        return VendorActor.Anonymous;
    }

    private static JsonResult Denied(string reasonCode, int statusCode)
        => new(new { reasonCode }) { StatusCode = statusCode };
}
