using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Nop.Web.Framework;
using Nop.Web.Framework.Controllers;
using Nop.Web.Framework.Mvc.Filters;
using TwinParticles.CheckEngine.Application.ImportPipeline.Orchestration;
using TwinParticles.CheckEngine.Infrastructure;
using TwinParticles.CheckEngine.Models;
using TwinParticles.CheckEngine.Security;

namespace TwinParticles.CheckEngine.Controllers;

[AuthorizeAdmin]
[Area(AreaNames.ADMIN)]
[AutoValidateAntiforgeryToken]
public sealed class ImportAdminController : BasePluginController
{
    private readonly ImportPipelineOrchestratorService _orchestrator;
    private readonly Nop.Services.Security.IPermissionService _permissionService;

    public ImportAdminController(ImportPipelineOrchestratorService orchestrator, Nop.Services.Security.IPermissionService permissionService)
    {
        _orchestrator = orchestrator;
        _permissionService = permissionService;
    }

    private async Task<bool> AuthorizedAsync() => await _permissionService.AuthorizeAsync(CheckEnginePermissionProvider.ManageCheckEngine.SystemName);

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        if (!await AuthorizedAsync()) return AccessDeniedView();
        return RedirectToAction(nameof(BatchBoard));
    }

    [HttpGet]
    public async Task<IActionResult> BatchBoard(Guid? batchId, CancellationToken cancellationToken)
    {
        if (!await AuthorizedAsync()) return AccessDeniedView();
        ViewBag.BatchId = batchId;
        return View("~/Plugins/TwinParticles.CheckEngine/Views/Admin/ImportBatch.cshtml");
    }

    [HttpPost]
    public async Task<IActionResult> Run([FromBody] ImportAdminRunRequestModel model, CancellationToken cancellationToken)
    {
        if (!await AuthorizedAsync()) return AccessDeniedView();

        var bytes = string.IsNullOrWhiteSpace(model.ContentBase64)
            ? Array.Empty<byte>()
            : Convert.FromBase64String(model.ContentBase64);

        var result = await _orchestrator.RunAsync(new ImportPipelineRunRequest
        {
            Format = model.Format,
            FileName = model.FileName,
            Content = bytes,
            SupplierProfileCode = model.SupplierProfileCode,
            DryRun = model.DryRun,
            EnableAiEnrichment = model.EnableAiEnrichment,
            EnableTranslation = model.EnableTranslation,
            EnableSeoGeneration = model.EnableSeoGeneration
        }, cancellationToken);

        return Json(result);
    }

    [HttpGet]
    public async Task<IActionResult> Batch(Guid batchId, CancellationToken cancellationToken)
    {
        if (!await AuthorizedAsync()) return AccessDeniedView();
        if (!Request.WantsJsonResponse())
            return RedirectToAction(nameof(BatchBoard), new { batchId });

        var batch = await _orchestrator.GetBatchAsync(batchId, cancellationToken);
        if (batch is null)
            return NotFound();

        return Json(batch);
    }

    [HttpPost]
    public async Task<IActionResult> SetReviewStatus([FromBody] ImportAdminReviewStatusModel model, CancellationToken cancellationToken)
    {
        if (!await AuthorizedAsync()) return AccessDeniedView();

        var updated = await _orchestrator.SetReviewStatusAsync(
            model.BatchId,
            model.RowNumber,
            model.ReviewStatus,
            actor: "admin",
            cancellationToken: cancellationToken);
        if (!updated)
            return NotFound();

        return Ok();
    }

    [HttpPost]
    public async Task<IActionResult> Publish([FromBody] ImportAdminPublishModel model, CancellationToken cancellationToken)
    {
        if (!await AuthorizedAsync()) return AccessDeniedView();

        var result = await _orchestrator.PublishAsync(model.BatchId, model.DryRun, cancellationToken);
        return Json(result);
    }

    [HttpPost]
    public async Task<IActionResult> SetDuplicateDecision([FromBody] ImportAdminDuplicateDecisionModel model, CancellationToken cancellationToken)
    {
        if (!await AuthorizedAsync()) return AccessDeniedView();

        var updated = await _orchestrator.SetDuplicateDecisionAsync(
            model.BatchId,
            model.RowNumber,
            model.Decision,
            actor: "admin",
            cancellationToken: cancellationToken);
        if (!updated)
            return BadRequest(new { reasonCode = "import.duplicate_decision.invalid" });

        return Ok();
    }

    [HttpPost]
    public async Task<IActionResult> RerunStage([FromBody] ImportAdminRerunStageModel model, CancellationToken cancellationToken)
    {
        if (!await AuthorizedAsync()) return AccessDeniedView();

        var result = await _orchestrator.RerunStageAsync(model.BatchId, model.Stage, cancellationToken);
        return Json(result);
    }
}
