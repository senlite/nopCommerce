using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Nop.Web.Framework;
using Nop.Web.Framework.Controllers;
using Nop.Web.Framework.Mvc.Filters;
using TwinParticles.CheckEngine.Application.ImportPipeline.Orchestration;
using TwinParticles.CheckEngine.Models;
using TwinParticles.CheckEngine.Security;

namespace TwinParticles.CheckEngine.Controllers;

[AuthorizeAdmin]
[Area(AreaNames.Admin)]
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
    public async Task<IActionResult> Batch(Guid batchId)
    {
        if (!await AuthorizedAsync()) return AccessDeniedView();

        var batch = _orchestrator.GetBatch(batchId);
        if (batch is null)
            return NotFound();

        return Json(batch);
    }

    [HttpPost]
    public async Task<IActionResult> SetReviewStatus([FromBody] ImportAdminReviewStatusModel model)
    {
        if (!await AuthorizedAsync()) return AccessDeniedView();

        var updated = _orchestrator.SetReviewStatus(model.BatchId, model.RowNumber, model.ReviewStatus);
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
}
