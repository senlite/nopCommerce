using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Nop.Services.Security;
using Nop.Web.Controllers;
using Nop.Web.Framework.Mvc.Filters;
using TwinParticles.CheckEngine.Application.Dealer;
using TwinParticles.CheckEngine.Application.Licensing;
using TwinParticles.CheckEngine.Domain.Dealer;
using TwinParticles.CheckEngine.Security;

namespace TwinParticles.CheckEngine.Controllers;

[AutoValidateAntiforgeryToken]
public sealed class DealerController : BasePublicController
{
    private readonly DealerPortalService _dealerService;
    private readonly DealerPortalLicenceGate _licenceGate;
    private readonly IPermissionService _permissionService;

    public DealerController(
        DealerPortalService dealerService,
        DealerPortalLicenceGate licenceGate,
        IPermissionService permissionService)
    {
        _dealerService = dealerService;
        _licenceGate = licenceGate;
        _permissionService = permissionService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        if (!await _licenceGate.AllowsDealerAsync(cancellationToken))
            return NotFound();

        return View("~/Plugins/TwinParticles.CheckEngine/Views/Dealer/Index.cshtml");
    }

    [HttpGet]
    public async Task<IActionResult> Catalog(int dealerAccountId, CancellationToken cancellationToken)
    {
        if (!await AuthorizedAsync(cancellationToken))
            return Denied(DealerErrorCodes.LicenceDenied);

        var result = await _dealerService.GetDealerCatalogViewAsync(dealerAccountId, cancellationToken);
        return result.Success ? Json(result) : Denied(result.ErrorCode ?? DealerErrorCodes.NotFound, 400);
    }

    [HttpPost]
    public async Task<IActionResult> PlaceDealerOrder([FromBody] PlaceDealerOrderRequest request, CancellationToken cancellationToken)
    {
        if (!await AuthorizedAsync(cancellationToken))
            return Denied(DealerErrorCodes.LicenceDenied);

        var result = await _dealerService.PlaceDealerOrderAsync(request, cancellationToken);
        return result.Success ? Json(result) : Denied(result.ErrorCode ?? DealerErrorCodes.NotFound, 400);
    }

    [HttpPost]
    public async Task<IActionResult> SubmitWarrantyClaim([FromBody] SubmitWarrantyClaimRequest request, CancellationToken cancellationToken)
    {
        if (!await AuthorizedAsync(cancellationToken))
            return Denied(DealerErrorCodes.LicenceDenied);

        var result = await _dealerService.SubmitWarrantyClaimAsync(request, cancellationToken);
        return result.Success ? Json(result) : Denied(result.ErrorCode ?? DealerErrorCodes.NotFound, 400);
    }

    [HttpPost]
    public async Task<IActionResult> TransitionWarrantyClaim([FromBody] TransitionWarrantyClaimRequest request, CancellationToken cancellationToken)
    {
        if (!await AuthorizedAsync(cancellationToken))
            return Denied(DealerErrorCodes.LicenceDenied);

        var result = await _dealerService.TransitionWarrantyClaimAsync(request.ClaimId, request.TargetStatus, cancellationToken);
        return result.Success ? Json(result) : Denied(result.ErrorCode ?? DealerErrorCodes.InvalidTransition, 400);
    }

    private async Task<bool> AuthorizedAsync(CancellationToken cancellationToken)
    {
        if (!await _licenceGate.AllowsDealerAsync(cancellationToken))
            return false;

        return await _permissionService.AuthorizeAsync(CheckEnginePermissionProvider.ManageCheckEngineDealer.SystemName);
    }

    private IActionResult Denied(string code, int statusCode = 403)
        => new JsonResult(new { success = false, errorCode = code }) { StatusCode = statusCode };
}

public sealed class TransitionWarrantyClaimRequest
{
    public int ClaimId { get; init; }

    public WarrantyClaimStatus TargetStatus { get; init; }
}
