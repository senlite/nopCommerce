using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Nop.Web.Framework;
using Nop.Web.Framework.Controllers;
using Nop.Web.Framework.Mvc.Filters;
using TwinParticles.CheckEngine.Application.Seo;
using TwinParticles.CheckEngine.Infrastructure;
using TwinParticles.CheckEngine.Models;
using TwinParticles.CheckEngine.Security;

namespace TwinParticles.CheckEngine.Controllers;

[AuthorizeAdmin]
[Area(AreaNames.ADMIN)]
[AutoValidateAntiforgeryToken]
public sealed class SeoAdminController : BasePluginController
{
    private readonly SeoLandingService _service;
    private readonly Nop.Services.Security.IPermissionService _permissionService;

    public SeoAdminController(SeoLandingService service, Nop.Services.Security.IPermissionService permissionService)
    {
        _service = service;
        _permissionService = permissionService;
    }

    private async Task<bool> AuthorizedAsync() => await _permissionService.AuthorizeAsync(CheckEnginePermissionProvider.ManageCheckEngine.SystemName);

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        if (!await AuthorizedAsync()) return AccessDeniedView();
        return View("~/Plugins/TwinParticles.CheckEngine/Views/Admin/SeoAdmin.cshtml");
    }

    [HttpPost]
    public async Task<IActionResult> GenerateVehicle([FromBody] SeoVehicleLandingRequestModel model, CancellationToken cancellationToken)
    {
        if (!await AuthorizedAsync()) return AccessDeniedView();

        try
        {
            var result = await _service.GenerateVehicleLandingAsync(model.VehicleConfigurationId, model.Locale, cancellationToken);
            return Json(result);
        }
        catch (System.Exception)
        {
            return Json(new TwinParticles.CheckEngine.Domain.Seo.SeoLandingGenerationResult
            {
                Success = false,
                ErrorCode = "seo.generate_failed"
            });
        }
    }

    [HttpPost]
    public async Task<IActionResult> GeneratePartForVehicle([FromBody] SeoPartForVehicleLandingRequestModel model, CancellationToken cancellationToken)
    {
        if (!await AuthorizedAsync()) return AccessDeniedView();

        try
        {
            var result = await _service.GeneratePartForVehicleLandingAsync(model.ProductId, model.VehicleConfigurationId, model.Locale, cancellationToken);
            return Json(result);
        }
        catch (System.Exception)
        {
            return Json(new TwinParticles.CheckEngine.Domain.Seo.SeoLandingGenerationResult
            {
                Success = false,
                ErrorCode = "seo.generate_failed"
            });
        }
    }

    [HttpPost]
    public async Task<IActionResult> RebuildSitemap(CancellationToken cancellationToken)
    {
        if (!await AuthorizedAsync()) return AccessDeniedView();

        await _service.RebuildSitemapAsync(cancellationToken);
        return Ok();
    }

    [HttpGet]
    public async Task<IActionResult> Sitemap(CancellationToken cancellationToken)
    {
        if (!await AuthorizedAsync()) return AccessDeniedView();
        if (!Request.WantsJsonResponse())
            return CheckEnginePaths.RedirectAdmin("SeoAdmin", "Index");

        var urls = await _service.GetSitemapUrlsAsync(cancellationToken);
        return Json(urls);
    }
}
