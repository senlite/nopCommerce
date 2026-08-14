using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Nop.Web.Framework;
using Nop.Web.Framework.Controllers;
using Nop.Web.Framework.Mvc.Filters;
using TwinParticles.CheckEngine.Application.ReferenceScale;
using TwinParticles.CheckEngine.Domain.ReferenceScale;
using TwinParticles.CheckEngine.Models;
using TwinParticles.CheckEngine.Security;

namespace TwinParticles.CheckEngine.Controllers;

[AuthorizeAdmin]
[Area(AreaNames.ADMIN)]
[AutoValidateAntiforgeryToken]
public sealed class ReferenceDataAdminController : BasePluginController
{
    private readonly ReferenceScaleCatalogService _service;
    private readonly Nop.Services.Security.IPermissionService _permissionService;

    public ReferenceDataAdminController(
        ReferenceScaleCatalogService service,
        Nop.Services.Security.IPermissionService permissionService)
    {
        _service = service;
        _permissionService = permissionService;
    }

    private async Task<bool> AuthorizedAsync()
        => await _permissionService.AuthorizeAsync(CheckEnginePermissionProvider.ManageCheckEngine.SystemName);

    [HttpGet]
    public async Task<IActionResult> Status(CancellationToken cancellationToken)
    {
        if (!await AuthorizedAsync())
            return AccessDeniedView();

        var status = await _service.GetStatusAsync(cancellationToken);
        return Json(new
        {
            manifest = new
            {
                ReferenceScaleManifest.Version,
                ReferenceScaleManifest.TargetProducts,
                ReferenceScaleManifest.TargetFitmentClaims,
                ReferenceScaleManifest.TargetConfigurations,
                ReferenceScaleManifest.TargetOemEntries,
                ReferenceScaleManifest.ProvenancePrefix
            },
            status
        });
    }

    [HttpPost]
    public async Task<IActionResult> Load([FromBody] ReferenceScaleLoadRequestModel model, CancellationToken cancellationToken)
    {
        if (!await AuthorizedAsync())
            return AccessDeniedView();

        var result = await _service.LoadAsync(new ReferenceScaleLoadRequest
        {
            ScaleFactor = model?.ScaleFactor ?? 1.0,
            ReplaceExisting = model?.ReplaceExisting ?? false,
            EnsureBmwVehicleSeed = model?.EnsureBmwVehicleSeed ?? true
        }, cancellationToken);

        return Json(result);
    }

    [HttpPost]
    public async Task<IActionResult> Purge(CancellationToken cancellationToken)
    {
        if (!await AuthorizedAsync())
            return AccessDeniedView();

        await _service.PurgeAsync(cancellationToken);
        return Ok();
    }
}
