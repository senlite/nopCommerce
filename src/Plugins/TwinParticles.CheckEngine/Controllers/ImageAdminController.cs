using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Nop.Web.Framework;
using Nop.Web.Framework.Controllers;
using Nop.Web.Framework.Mvc.Filters;
using TwinParticles.CheckEngine.Application.Images;
using TwinParticles.CheckEngine.Models;
using TwinParticles.CheckEngine.Security;

namespace TwinParticles.CheckEngine.Controllers;

[AuthorizeAdmin]
[Area(AreaNames.ADMIN)]
[AutoValidateAntiforgeryToken]
public sealed class ImageAdminController : BasePluginController
{
    private readonly ProductImageService _service;
    private readonly Nop.Services.Security.IPermissionService _permissionService;

    public ImageAdminController(ProductImageService service, Nop.Services.Security.IPermissionService permissionService)
    {
        _service = service;
        _permissionService = permissionService;
    }

    private async Task<bool> AuthorizedAsync() => await _permissionService.AuthorizeAsync(CheckEnginePermissionProvider.ManageCheckEngine.SystemName);

    [HttpPost]
    public async Task<IActionResult> Replace([FromBody] ImageReplaceRequestModel model, CancellationToken cancellationToken)
    {
        if (!await AuthorizedAsync()) return AccessDeniedView();

        var result = await _service.ReplacePrimaryAsync(model.ProductId, model.SourceUrl, model.SeoName, model.AltTextEn, model.AltTextAr, cancellationToken);
        return Json(result);
    }
}
