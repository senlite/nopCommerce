using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Nop.Core;
using Nop.Services.Customers;
using Nop.Services.Security;
using Nop.Web.Controllers;
using Nop.Web.Framework.Mvc.Filters;
using TwinParticles.CheckEngine.Application.Licensing;
using TwinParticles.CheckEngine.Application.Portals;
using TwinParticles.CheckEngine.Application.Workshop;
using TwinParticles.CheckEngine.Domain.Workshop;
using TwinParticles.CheckEngine.Security;

namespace TwinParticles.CheckEngine.Controllers;

[AutoValidateAntiforgeryToken]
public sealed class WorkshopController : BasePublicController
{
    private readonly WorkshopJobService _jobService;
    private readonly WorkshopPortalLicenceGate _licenceGate;
    private readonly VerticalPortalAccessService _portalAccess;
    private readonly IPermissionService _permissionService;
    private readonly ICustomerService _customerService;
    private readonly IWorkContext _workContext;

    public WorkshopController(
        WorkshopJobService jobService,
        WorkshopPortalLicenceGate licenceGate,
        VerticalPortalAccessService portalAccess,
        IPermissionService permissionService,
        ICustomerService customerService,
        IWorkContext workContext)
    {
        _jobService = jobService;
        _licenceGate = licenceGate;
        _portalAccess = portalAccess;
        _permissionService = permissionService;
        _customerService = customerService;
        _workContext = workContext;
    }

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        if (!await _licenceGate.AllowsWorkshopAsync(cancellationToken))
            return NotFound();

        return View("~/Plugins/TwinParticles.CheckEngine/Views/Workshop/Index.cshtml");
    }

    [HttpGet]
    public async Task<IActionResult> DashboardData(CancellationToken cancellationToken)
    {
        var access = await ResolveAccessAsync(cancellationToken);
        if (!access.Allowed)
            return Denied(access.ErrorCode ?? PortalErrorCodes.AccessDenied, StatusFor(access.ErrorCode));

        var customer = await _workContext.GetCurrentCustomerAsync();
        var snapshot = await _jobService.GetDashboardAsync(customer.Id, cancellationToken);
        return snapshot is null ? Denied(WorkshopErrorCodes.NotFound, 404) : Json(snapshot);
    }

    [HttpGet]
    public async Task<IActionResult> JobDetail(int jobId, CancellationToken cancellationToken)
    {
        var access = await ResolveAccessAsync(cancellationToken);
        if (!access.Allowed)
            return Denied(access.ErrorCode ?? PortalErrorCodes.AccessDenied, StatusFor(access.ErrorCode));

        var detail = await _jobService.GetJobDetailAsync(jobId, cancellationToken);
        if (detail is null)
            return Denied(WorkshopErrorCodes.NotFound, 404);

        var customer = await _workContext.GetCurrentCustomerAsync();
        var isOperator = await IsOperatorAsync();
        if (!await _portalAccess.OwnsWorkshopAccountAsync(customer.Id, detail.Job.WorkshopAccountId, isOperator, cancellationToken))
            return Denied(PortalErrorCodes.AccessDenied);

        return Json(detail);
    }

    [HttpPost]
    public async Task<IActionResult> CreateJob([FromBody] CreateJobRequest request, CancellationToken cancellationToken)
    {
        var access = await ResolveAccessAsync(cancellationToken);
        if (!access.Allowed)
            return Denied(access.ErrorCode ?? PortalErrorCodes.AccessDenied, StatusFor(access.ErrorCode));

        var customer = await _workContext.GetCurrentCustomerAsync();
        var isOperator = await IsOperatorAsync();
        if (!await _portalAccess.OwnsWorkshopAccountAsync(customer.Id, request.WorkshopAccountId, isOperator, cancellationToken))
            return Denied(PortalErrorCodes.AccessDenied);

        var result = await _jobService.CreateJobAsync(request, cancellationToken);
        return result.Success ? Json(result) : Denied(result.ErrorCode ?? WorkshopErrorCodes.NotFound, 400);
    }

    [HttpPost]
    public async Task<IActionResult> AllocateJobLine([FromBody] AllocateJobLineRequest request, CancellationToken cancellationToken)
    {
        var access = await ResolveAccessAsync(cancellationToken);
        if (!access.Allowed)
            return Denied(access.ErrorCode ?? PortalErrorCodes.AccessDenied, StatusFor(access.ErrorCode));

        var detail = await _jobService.GetJobDetailAsync(request.JobId, cancellationToken);
        if (detail is null)
            return Denied(WorkshopErrorCodes.NotFound, 404);

        var customer = await _workContext.GetCurrentCustomerAsync();
        var isOperator = await IsOperatorAsync();
        if (!await _portalAccess.OwnsWorkshopAccountAsync(customer.Id, detail.Job.WorkshopAccountId, isOperator, cancellationToken))
            return Denied(PortalErrorCodes.AccessDenied);

        var result = await _jobService.AllocateJobLineAsync(request, cancellationToken);
        return result.Success ? Json(result) : Denied(result.ErrorCode ?? WorkshopErrorCodes.NotFound, 400);
    }

    [HttpPost]
    public async Task<IActionResult> TransitionJobStatus([FromBody] TransitionJobStatusRequest request, CancellationToken cancellationToken)
    {
        var access = await ResolveAccessAsync(cancellationToken);
        if (!access.Allowed)
            return Denied(access.ErrorCode ?? PortalErrorCodes.AccessDenied, StatusFor(access.ErrorCode));

        var detail = await _jobService.GetJobDetailAsync(request.JobId, cancellationToken);
        if (detail is null)
            return Denied(WorkshopErrorCodes.NotFound, 404);

        var customer = await _workContext.GetCurrentCustomerAsync();
        var isOperator = await IsOperatorAsync();
        if (!await _portalAccess.OwnsWorkshopAccountAsync(customer.Id, detail.Job.WorkshopAccountId, isOperator, cancellationToken))
            return Denied(PortalErrorCodes.AccessDenied);

        var result = await _jobService.TransitionJobStatusAsync(request.JobId, request.TargetStatus, cancellationToken);
        return result.Success ? Json(result) : Denied(result.ErrorCode ?? WorkshopErrorCodes.InvalidTransition, 400);
    }

    [HttpPost]
    public async Task<IActionResult> RaiseJobInvoice([FromBody] RaiseJobInvoiceRequest request, CancellationToken cancellationToken)
    {
        var access = await ResolveAccessAsync(cancellationToken);
        if (!access.Allowed)
            return Denied(access.ErrorCode ?? PortalErrorCodes.AccessDenied, StatusFor(access.ErrorCode));

        var detail = await _jobService.GetJobDetailAsync(request.JobId, cancellationToken);
        if (detail is null)
            return Denied(WorkshopErrorCodes.NotFound, 404);

        var customer = await _workContext.GetCurrentCustomerAsync();
        var isOperator = await IsOperatorAsync();
        if (!await _portalAccess.OwnsWorkshopAccountAsync(customer.Id, detail.Job.WorkshopAccountId, isOperator, cancellationToken))
            return Denied(PortalErrorCodes.AccessDenied);

        if (!await _portalAccess.CanRaiseWorkshopInvoiceAsync(customer.Id, detail.Job.WorkshopAccountId, isOperator, cancellationToken))
            return Denied(WorkshopErrorCodes.InvoiceDenied);

        var result = await _jobService.RaiseJobInvoiceAsync(request.JobId, request.JobVehicleId, cancellationToken);
        return result.Success ? Json(result) : Denied(result.ErrorCode ?? WorkshopErrorCodes.NotFound, 400);
    }

    [HttpPost]
    public async Task<IActionResult> CreateWorkshopCustomer([FromBody] CreateWorkshopCustomerRequest request, CancellationToken cancellationToken)
    {
        var access = await ResolveAccessAsync(cancellationToken);
        if (!access.Allowed)
            return Denied(access.ErrorCode ?? PortalErrorCodes.AccessDenied, StatusFor(access.ErrorCode));

        var customer = await _workContext.GetCurrentCustomerAsync();
        var isOperator = await IsOperatorAsync();
        if (!await _portalAccess.OwnsWorkshopAccountAsync(customer.Id, request.WorkshopAccountId, isOperator, cancellationToken))
            return Denied(PortalErrorCodes.AccessDenied);

        var result = await _jobService.CreateWorkshopCustomerAsync(request, cancellationToken);
        return result.Success ? Json(result) : Denied(result.ErrorCode ?? WorkshopErrorCodes.NotFound, 400);
    }

    [HttpPost]
    public async Task<IActionResult> AddWorkshopCustomerVehicle([FromBody] AddWorkshopCustomerVehicleRequest request, CancellationToken cancellationToken)
    {
        var access = await ResolveAccessAsync(cancellationToken);
        if (!access.Allowed)
            return Denied(access.ErrorCode ?? PortalErrorCodes.AccessDenied, StatusFor(access.ErrorCode));

        var result = await _jobService.AddWorkshopCustomerVehicleAsync(request, cancellationToken);
        return result.Success ? Json(result) : Denied(result.ErrorCode ?? WorkshopErrorCodes.NotFound, 400);
    }

    private async Task<PortalAccessResult> ResolveAccessAsync(CancellationToken cancellationToken)
    {
        if (!await _licenceGate.AllowsWorkshopAsync(cancellationToken))
            return PortalAccessResult.Denied(WorkshopErrorCodes.LicenceDenied);

        var customer = await _workContext.GetCurrentCustomerAsync();
        if (await _customerService.IsGuestAsync(customer))
            return PortalAccessResult.Denied(PortalErrorCodes.AccessUnauthenticated);

        var isOperator = await IsOperatorAsync();
        return await _portalAccess.ResolveWorkshopAsync(customer.Id, isOperator, cancellationToken);
    }

    private Task<bool> IsOperatorAsync()
        => _permissionService.AuthorizeAsync(CheckEnginePermissionProvider.ManageCheckEngine.SystemName);

    private static int StatusFor(string? errorCode)
        => errorCode == PortalErrorCodes.AccessUnauthenticated ? 401 : 403;

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

    public int? JobVehicleId { get; init; }
}
