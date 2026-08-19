using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Nop.Web.Framework;
using Nop.Web.Framework.Controllers;
using Nop.Web.Framework.Mvc.Filters;
using TwinParticles.CheckEngine.Application.Erp;
using TwinParticles.CheckEngine.Domain.Erp;
using TwinParticles.CheckEngine.Infrastructure;
using TwinParticles.CheckEngine.Models;
using TwinParticles.CheckEngine.Security;

namespace TwinParticles.CheckEngine.Controllers;

[AuthorizeAdmin]
[Area(AreaNames.ADMIN)]
[AutoValidateAntiforgeryToken]
public sealed class ErpAdminController : BasePluginController
{
    private readonly ErpSyncService _service;
    private readonly Nop.Services.Security.IPermissionService _permissionService;

    public ErpAdminController(ErpSyncService service, Nop.Services.Security.IPermissionService permissionService)
    {
        _service = service;
        _permissionService = permissionService;
    }

    private async Task<bool> AuthorizedAsync() => await _permissionService.AuthorizeAsync(CheckEnginePermissionProvider.ManageCheckEngine.SystemName);

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        if (!await AuthorizedAsync()) return AccessDeniedView();
        return View("~/Plugins/TwinParticles.CheckEngine/Views/Admin/ErpAdmin.cshtml");
    }

    [HttpPost]
    public async Task<IActionResult> Queue([FromBody] ErpQueueRequestModel model, CancellationToken cancellationToken)
    {
        if (!await AuthorizedAsync()) return AccessDeniedView();

        var jobId = await _service.QueueSyncAsync(model.EntityType, model.Direction, model.LocalId, model.Payload, cancellationToken);
        return Json(new { jobId });
    }

    [HttpPost]
    public async Task<IActionResult> Process(CancellationToken cancellationToken)
    {
        if (!await AuthorizedAsync()) return AccessDeniedView();

        var processed = await _service.ProcessPendingAsync(cancellationToken);
        return Json(new { processed });
    }

    [HttpGet]
    public async Task<IActionResult> Reconcile(CancellationToken cancellationToken)
    {
        if (!await AuthorizedAsync()) return AccessDeniedView();
        if (!Request.WantsJsonResponse())
            return CheckEnginePaths.RedirectAdmin("ErpAdmin", "Index");

        var toUtc = System.DateTime.UtcNow;
        return Json(await _service.BuildReconciliationReportAsync(toUtc.AddDays(-1), toUtc, cancellationToken));
    }

    [HttpGet]
    public async Task<IActionResult> InventorySnapshot(CancellationToken cancellationToken)
    {
        if (!await AuthorizedAsync()) return AccessDeniedView();
        if (!Request.WantsJsonResponse())
            return CheckEnginePaths.RedirectAdmin("ErpAdmin", "Index");

        var payload = await _service.PullInventorySnapshotAsync(cancellationToken);
        return Json(new { payload });
    }
}
