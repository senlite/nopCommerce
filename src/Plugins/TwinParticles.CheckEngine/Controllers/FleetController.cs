using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Nop.Core;
using Nop.Services.Customers;
using Nop.Services.Security;
using Nop.Web.Controllers;
using Nop.Web.Framework.Mvc.Filters;
using TwinParticles.CheckEngine.Application.Fleet;
using TwinParticles.CheckEngine.Application.Licensing;
using TwinParticles.CheckEngine.Application.Portals;
using TwinParticles.CheckEngine.Domain.Fleet;
using TwinParticles.CheckEngine.Security;

namespace TwinParticles.CheckEngine.Controllers;

[AutoValidateAntiforgeryToken]
public sealed class FleetController : BasePublicController
{
    private readonly FleetPortalService _fleetService;
    private readonly FleetPortalLicenceGate _licenceGate;
    private readonly VerticalPortalAccessService _portalAccess;
    private readonly IPermissionService _permissionService;
    private readonly ICustomerService _customerService;
    private readonly IWorkContext _workContext;

    public FleetController(
        FleetPortalService fleetService,
        FleetPortalLicenceGate licenceGate,
        VerticalPortalAccessService portalAccess,
        IPermissionService permissionService,
        ICustomerService customerService,
        IWorkContext workContext)
    {
        _fleetService = fleetService;
        _licenceGate = licenceGate;
        _portalAccess = portalAccess;
        _permissionService = permissionService;
        _customerService = customerService;
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
        var access = await ResolveAccessAsync(cancellationToken);
        if (!access.Allowed)
            return Denied(access.ErrorCode ?? PortalErrorCodes.AccessDenied, StatusFor(access.ErrorCode));

        var customer = await _workContext.GetCurrentCustomerAsync();
        var snapshot = await _fleetService.GetDashboardAsync(customer.Id, cancellationToken);
        return snapshot is null ? Denied(FleetErrorCodes.NotFound, 404) : Json(snapshot);
    }

    [HttpPost]
    public async Task<IActionResult> ImportFleetVins([FromBody] ImportFleetVinsRequest request, CancellationToken cancellationToken)
    {
        var access = await ResolveAccessAsync(cancellationToken);
        if (!access.Allowed)
            return Denied(access.ErrorCode ?? PortalErrorCodes.AccessDenied, StatusFor(access.ErrorCode));

        var customer = await _workContext.GetCurrentCustomerAsync();
        var isOperator = await IsOperatorAsync();
        if (!await _portalAccess.OwnsFleetAccountAsync(customer.Id, request.FleetAccountId, isOperator, cancellationToken))
            return Denied(PortalErrorCodes.AccessDenied);

        var result = await _fleetService.ImportFleetVinsAsync(request.FleetAccountId, request.Vins, cancellationToken);
        return result.Success ? Json(result) : Denied(result.ErrorCode ?? FleetErrorCodes.NotFound, 400);
    }

    [HttpPost]
    public async Task<IActionResult> SubmitApprovalRequest([FromBody] SubmitApprovalRequest request, CancellationToken cancellationToken)
    {
        var access = await ResolveAccessAsync(cancellationToken);
        if (!access.Allowed)
            return Denied(access.ErrorCode ?? PortalErrorCodes.AccessDenied, StatusFor(access.ErrorCode));

        var customer = await _workContext.GetCurrentCustomerAsync();
        var isOperator = await IsOperatorAsync();
        if (!await _portalAccess.OwnsFleetAccountAsync(customer.Id, request.FleetAccountId, isOperator, cancellationToken))
            return Denied(PortalErrorCodes.AccessDenied);

        var result = await _fleetService.SubmitApprovalRequestAsync(request, cancellationToken);
        return result.Success ? Json(result) : Denied(result.ErrorCode ?? FleetErrorCodes.NotFound, 400);
    }

    [HttpPost]
    public async Task<IActionResult> DecideApprovalRequest([FromBody] DecideApprovalRequestModel request, CancellationToken cancellationToken)
    {
        var access = await ResolveAccessAsync(cancellationToken);
        if (!access.Allowed)
            return Denied(access.ErrorCode ?? PortalErrorCodes.AccessDenied, StatusFor(access.ErrorCode));

        var customer = await _workContext.GetCurrentCustomerAsync();
        var isOperator = await IsOperatorAsync();
        var fleetAccountId = await _fleetService.ResolveFleetAccountIdForRequestAsync(request.RequestId, cancellationToken);
        if (!fleetAccountId.HasValue || !await _portalAccess.OwnsFleetAccountAsync(customer.Id, fleetAccountId.Value, isOperator, cancellationToken))
            return Denied(PortalErrorCodes.AccessDenied);

        var result = await _fleetService.DecideApprovalRequestAsync(
            request.RequestId,
            request.Approve,
            request.RejectionReason,
            cancellationToken);

        return result.Success ? Json(result) : Denied(result.ErrorCode ?? FleetErrorCodes.InvalidDecision, 400);
    }

    private async Task<PortalAccessResult> ResolveAccessAsync(CancellationToken cancellationToken)
    {
        if (!await _licenceGate.AllowsFleetAsync(cancellationToken))
            return PortalAccessResult.Denied(FleetErrorCodes.LicenceDenied);

        var customer = await _workContext.GetCurrentCustomerAsync();
        if (await _customerService.IsGuestAsync(customer))
            return PortalAccessResult.Denied(PortalErrorCodes.AccessUnauthenticated);

        var isOperator = await IsOperatorAsync();
        return await _portalAccess.ResolveFleetAsync(customer.Id, isOperator, cancellationToken);
    }

    private Task<bool> IsOperatorAsync()
        => _permissionService.AuthorizeAsync(CheckEnginePermissionProvider.ManageCheckEngine.SystemName);

    private static int StatusFor(string? errorCode)
        => errorCode == PortalErrorCodes.AccessUnauthenticated ? 401 : 403;

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
