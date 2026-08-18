using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Nop.Core;
using Nop.Services.Security;
using Nop.Web.Controllers;
using Nop.Web.Framework.Mvc.Filters;
using TwinParticles.CheckEngine.Application.Fleet;
using TwinParticles.CheckEngine.Application.Licensing;
using TwinParticles.CheckEngine.Domain.Fleet;
using TwinParticles.CheckEngine.Security;

namespace TwinParticles.CheckEngine.Controllers;

[AutoValidateAntiforgeryToken]
public sealed class FleetController : BasePublicController
{
    private readonly FleetPortalService _fleetService;
    private readonly FleetPortalLicenceGate _licenceGate;
    private readonly IPermissionService _permissionService;
    private readonly IWorkContext _workContext;

    public FleetController(
        FleetPortalService fleetService,
        FleetPortalLicenceGate licenceGate,
        IPermissionService permissionService,
        IWorkContext workContext)
    {
        _fleetService = fleetService;
        _licenceGate = licenceGate;
        _permissionService = permissionService;
        _workContext = workContext;
    }

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        if (!await _licenceGate.AllowsFleetAsync(cancellationToken))
            return NotFound();

        return View("~/Plugins/TwinParticles.CheckEngine/Views/Fleet/Index.cshtml");
    }

    [HttpGet]
    public async Task<IActionResult> DashboardData(CancellationToken cancellationToken)
    {
        if (!await AuthorizedAsync(cancellationToken))
            return Denied(FleetErrorCodes.LicenceDenied);

        var customer = await _workContext.GetCurrentCustomerAsync();
        var snapshot = await _fleetService.GetDashboardAsync(customer.Id, cancellationToken);
        return snapshot is null ? Denied(FleetErrorCodes.NotFound, 404) : Json(snapshot);
    }

    [HttpPost]
    public async Task<IActionResult> ImportFleetVins([FromBody] ImportFleetVinsRequest request, CancellationToken cancellationToken)
    {
        if (!await AuthorizedAsync(cancellationToken))
            return Denied(FleetErrorCodes.LicenceDenied);

        var result = await _fleetService.ImportFleetVinsAsync(request.FleetAccountId, request.Vins, cancellationToken);
        return result.Success ? Json(result) : Denied(result.ErrorCode ?? FleetErrorCodes.NotFound, 400);
    }

    [HttpPost]
    public async Task<IActionResult> SubmitApprovalRequest([FromBody] SubmitApprovalRequest request, CancellationToken cancellationToken)
    {
        if (!await AuthorizedAsync(cancellationToken))
            return Denied(FleetErrorCodes.LicenceDenied);

        var result = await _fleetService.SubmitApprovalRequestAsync(request, cancellationToken);
        return result.Success ? Json(result) : Denied(result.ErrorCode ?? FleetErrorCodes.NotFound, 400);
    }

    [HttpPost]
    public async Task<IActionResult> DecideApprovalRequest([FromBody] DecideApprovalRequestModel request, CancellationToken cancellationToken)
    {
        if (!await AuthorizedAsync(cancellationToken))
            return Denied(FleetErrorCodes.LicenceDenied);

        var result = await _fleetService.DecideApprovalRequestAsync(
            request.RequestId,
            request.Approve,
            request.RejectionReason,
            cancellationToken);

        return result.Success ? Json(result) : Denied(result.ErrorCode ?? FleetErrorCodes.InvalidDecision, 400);
    }

    private async Task<bool> AuthorizedAsync(CancellationToken cancellationToken)
    {
        if (!await _licenceGate.AllowsFleetAsync(cancellationToken))
            return false;

        return await _permissionService.AuthorizeAsync(CheckEnginePermissionProvider.ManageCheckEngineFleet.SystemName);
    }

    private IActionResult Denied(string code, int statusCode = 403)
        => new JsonResult(new { success = false, errorCode = code }) { StatusCode = statusCode };
}

public sealed class ImportFleetVinsRequest
{
    public int FleetAccountId { get; init; }

    public string[] Vins { get; init; } = [];
}

public sealed class DecideApprovalRequestModel
{
    public int RequestId { get; init; }

    public bool Approve { get; init; }

    public string? RejectionReason { get; init; }
}
