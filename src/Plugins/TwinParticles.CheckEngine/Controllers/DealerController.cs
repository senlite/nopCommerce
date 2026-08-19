using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Nop.Core;
using Nop.Services.Customers;
using Nop.Services.Security;
using Nop.Web.Controllers;
using Nop.Web.Framework.Mvc.Filters;
using TwinParticles.CheckEngine.Application.Dealer;
using TwinParticles.CheckEngine.Application.Licensing;
using TwinParticles.CheckEngine.Application.Portals;
using TwinParticles.CheckEngine.Domain.Dealer;
using TwinParticles.CheckEngine.Infrastructure;
using TwinParticles.CheckEngine.Security;

namespace TwinParticles.CheckEngine.Controllers;

[AutoValidateAntiforgeryToken]
public sealed class DealerController : BasePublicController
{
    private readonly DealerPortalService _dealerService;
    private readonly DealerPortalLicenceGate _licenceGate;
    private readonly VerticalPortalAccessService _portalAccess;
    private readonly IPermissionService _permissionService;
    private readonly ICustomerService _customerService;
    private readonly IWorkContext _workContext;

    public DealerController(
        DealerPortalService dealerService,
        DealerPortalLicenceGate licenceGate,
        VerticalPortalAccessService portalAccess,
        IPermissionService permissionService,
        ICustomerService customerService,
        IWorkContext workContext)
    {
        _dealerService = dealerService;
        _licenceGate = licenceGate;
        _portalAccess = portalAccess;
        _permissionService = permissionService;
        _customerService = customerService;
        _workContext = workContext;
    }

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        if (!await _licenceGate.AllowsDealerAsync(cancellationToken))
            return NotFound();

        return View("~/Plugins/TwinParticles.CheckEngine/Views/Dealer/Index.cshtml");
    }

    private IActionResult? PortalPageOrJson()
        => Request.WantsJsonResponse() ? null : CheckEnginePaths.RedirectPortal("dealer", "Index");

    [HttpGet]
    public async Task<IActionResult> DashboardData(CancellationToken cancellationToken)
    {
        if (PortalPageOrJson() is { } page)
            return page;

        var access = await ResolveAccessAsync(cancellationToken);
        if (!access.Allowed)
            return Denied(access.ErrorCode ?? PortalErrorCodes.AccessDenied, StatusFor(access.ErrorCode));

        var customer = await _workContext.GetCurrentCustomerAsync();
        var snapshot = await _dealerService.GetDashboardAsync(customer.Id, cancellationToken);
        return snapshot is null ? Denied(DealerErrorCodes.NotFound, 404) : Json(snapshot);
    }

    [HttpGet]
    public async Task<IActionResult> Catalog(int dealerAccountId, CancellationToken cancellationToken)
    {
        if (PortalPageOrJson() is { } page)
            return page;

        var access = await ResolveAccessAsync(cancellationToken);
        if (!access.Allowed)
            return Denied(access.ErrorCode ?? PortalErrorCodes.AccessDenied, StatusFor(access.ErrorCode));

        var customer = await _workContext.GetCurrentCustomerAsync();
        var isOperator = await IsOperatorAsync();
        if (!await _portalAccess.OwnsDealerAccountAsync(customer.Id, dealerAccountId, isOperator, cancellationToken))
            return Denied(PortalErrorCodes.AccessDenied);

        var result = await _dealerService.GetDealerCatalogViewAsync(dealerAccountId, cancellationToken);
        return result.Success ? Json(result) : Denied(result.ErrorCode ?? DealerErrorCodes.NotFound, 400);
    }

    [HttpPost]
    public async Task<IActionResult> PlaceDealerOrder([FromBody] PlaceDealerOrderRequest request, CancellationToken cancellationToken)
    {
        var access = await ResolveAccessAsync(cancellationToken);
        if (!access.Allowed)
            return Denied(access.ErrorCode ?? PortalErrorCodes.AccessDenied, StatusFor(access.ErrorCode));

        var customer = await _workContext.GetCurrentCustomerAsync();
        var isOperator = await IsOperatorAsync();
        if (!await _portalAccess.OwnsDealerAccountAsync(customer.Id, request.DealerAccountId, isOperator, cancellationToken))
            return Denied(PortalErrorCodes.AccessDenied);

        var result = await _dealerService.PlaceDealerOrderAsync(request, cancellationToken);
        return result.Success ? Json(result) : Denied(result.ErrorCode ?? DealerErrorCodes.NotFound, 400);
    }

    [HttpPost]
    public async Task<IActionResult> SubmitWarrantyClaim([FromBody] SubmitWarrantyClaimRequest request, CancellationToken cancellationToken)
    {
        var access = await ResolveAccessAsync(cancellationToken);
        if (!access.Allowed)
            return Denied(access.ErrorCode ?? PortalErrorCodes.AccessDenied, StatusFor(access.ErrorCode));

        var customer = await _workContext.GetCurrentCustomerAsync();
        var isOperator = await IsOperatorAsync();
        if (!await _portalAccess.OwnsDealerAccountAsync(customer.Id, request.DealerAccountId, isOperator, cancellationToken))
            return Denied(PortalErrorCodes.AccessDenied);

        var result = await _dealerService.SubmitWarrantyClaimAsync(request, cancellationToken);
        return result.Success ? Json(result) : Denied(result.ErrorCode ?? DealerErrorCodes.NotFound, 400);
    }

    [HttpPost]
    public async Task<IActionResult> TransitionWarrantyClaim([FromBody] TransitionWarrantyClaimRequest request, CancellationToken cancellationToken)
    {
        var access = await ResolveAccessAsync(cancellationToken);
        if (!access.Allowed)
            return Denied(access.ErrorCode ?? PortalErrorCodes.AccessDenied, StatusFor(access.ErrorCode));

        var customer = await _workContext.GetCurrentCustomerAsync();
        var isOperator = await IsOperatorAsync();
        var dealerAccountId = await _dealerService.ResolveDealerAccountIdForClaimAsync(request.ClaimId, cancellationToken);
        if (!dealerAccountId.HasValue || !await _portalAccess.OwnsDealerAccountAsync(customer.Id, dealerAccountId.Value, isOperator, cancellationToken))
            return Denied(PortalErrorCodes.AccessDenied);

        var result = await _dealerService.TransitionWarrantyClaimAsync(request.ClaimId, request.TargetStatus, cancellationToken);
        return result.Success ? Json(result) : Denied(result.ErrorCode ?? DealerErrorCodes.InvalidTransition, 400);
    }

    private async Task<PortalAccessResult> ResolveAccessAsync(CancellationToken cancellationToken)
    {
        if (!await _licenceGate.AllowsDealerAsync(cancellationToken))
            return PortalAccessResult.Denied(DealerErrorCodes.LicenceDenied);

        var customer = await _workContext.GetCurrentCustomerAsync();
        if (await _customerService.IsGuestAsync(customer))
            return PortalAccessResult.Denied(PortalErrorCodes.AccessUnauthenticated);

        var isOperator = await IsOperatorAsync();
        return await _portalAccess.ResolveDealerAsync(customer.Id, isOperator, cancellationToken);
    }

    private async Task<bool> IsOperatorAsync()
    {
        if (await _permissionService.AuthorizeAsync(CheckEnginePermissionProvider.ManageCheckEngine.SystemName))
            return true;

        return await _permissionService.AuthorizeAsync(CheckEnginePermissionProvider.ManageCheckEngineDealer.SystemName);
    }

    private static int StatusFor(string? errorCode)
        => errorCode == PortalErrorCodes.AccessUnauthenticated ? 401 : 403;

    private IActionResult Denied(string code, int statusCode = 403)
        => new JsonResult(new { success = false, errorCode = code }) { StatusCode = statusCode };
}

public sealed class TransitionWarrantyClaimRequest
{
    public int ClaimId { get; init; }

    public WarrantyClaimStatus TargetStatus { get; init; }
}
