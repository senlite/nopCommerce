using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Nop.Services.Security;
using Nop.Web.Controllers;
using Nop.Web.Framework.Mvc.Filters;
using TwinParticles.CheckEngine.Application.Licensing;
using TwinParticles.CheckEngine.Application.Workshop;
using TwinParticles.CheckEngine.Domain.Workshop;
using TwinParticles.CheckEngine.Security;

namespace TwinParticles.CheckEngine.Controllers;

[AutoValidateAntiforgeryToken]
public sealed class WorkshopController : BasePublicController
{
    private readonly WorkshopJobService _jobService;
    private readonly WorkshopPortalLicenceGate _licenceGate;
    private readonly IPermissionService _permissionService;

    public WorkshopController(
        WorkshopJobService jobService,
        WorkshopPortalLicenceGate licenceGate,
        IPermissionService permissionService)
    {
        _jobService = jobService;
        _licenceGate = licenceGate;
        _permissionService = permissionService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        if (!await _licenceGate.AllowsWorkshopAsync(cancellationToken))
            return NotFound();

        return View("~/Plugins/TwinParticles.CheckEngine/Views/Workshop/Index.cshtml");
    }

    [HttpPost]
    public async Task<IActionResult> CreateJob([FromBody] CreateJobRequest request, CancellationToken cancellationToken)
    {
        if (!await AuthorizedAsync(cancellationToken))
            return Denied(WorkshopErrorCodes.LicenceDenied);

        var result = await _jobService.CreateJobAsync(request, cancellationToken);
        return result.Success ? Json(result) : Denied(result.ErrorCode ?? WorkshopErrorCodes.NotFound, 400);
    }

    [HttpPost]
    public async Task<IActionResult> AllocateJobLine([FromBody] AllocateJobLineRequest request, CancellationToken cancellationToken)
    {
        if (!await AuthorizedAsync(cancellationToken))
            return Denied(WorkshopErrorCodes.LicenceDenied);

        var result = await _jobService.AllocateJobLineAsync(request, cancellationToken);
        return result.Success ? Json(result) : Denied(result.ErrorCode ?? WorkshopErrorCodes.NotFound, 400);
    }

    [HttpPost]
    public async Task<IActionResult> TransitionJobStatus([FromBody] TransitionJobStatusRequest request, CancellationToken cancellationToken)
    {
        if (!await AuthorizedAsync(cancellationToken))
            return Denied(WorkshopErrorCodes.LicenceDenied);

        var result = await _jobService.TransitionJobStatusAsync(request.JobId, request.TargetStatus, cancellationToken);
        return result.Success ? Json(result) : Denied(result.ErrorCode ?? WorkshopErrorCodes.InvalidTransition, 400);
    }

    [HttpPost]
    public async Task<IActionResult> RaiseJobInvoice([FromBody] RaiseJobInvoiceRequest request, CancellationToken cancellationToken)
    {
        if (!await AuthorizedAsync(cancellationToken))
            return Denied(WorkshopErrorCodes.LicenceDenied);

        var result = await _jobService.RaiseJobInvoiceAsync(request.JobId, cancellationToken);
        return result.Success ? Json(result) : Denied(result.ErrorCode ?? WorkshopErrorCodes.NotFound, 400);
    }

    private async Task<bool> AuthorizedAsync(CancellationToken cancellationToken)
    {
        if (!await _licenceGate.AllowsWorkshopAsync(cancellationToken))
            return false;

        return await _permissionService.AuthorizeAsync(CheckEnginePermissionProvider.ManageCheckEngineWorkshop.SystemName);
    }

    private IActionResult Denied(string code, int statusCode = 403)
        => new JsonResult(new { success = false, errorCode = code }) { StatusCode = statusCode };
}

public sealed class TransitionJobStatusRequest
{
    public int JobId { get; init; }

    public WorkshopJobStatus TargetStatus { get; init; }
}

public sealed class RaiseJobInvoiceRequest
{
    public int JobId { get; init; }
}
