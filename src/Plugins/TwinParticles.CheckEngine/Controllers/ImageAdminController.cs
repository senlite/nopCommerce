using System.Linq;
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
    private readonly BatchImageReplacementService _batchService;
    private readonly Nop.Services.Security.IPermissionService _permissionService;

    public ImageAdminController(
        ProductImageService service,
        BatchImageReplacementService batchService,
        Nop.Services.Security.IPermissionService permissionService)
    {
        _service = service;
        _batchService = batchService;
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

    [HttpPost]
    public async Task<IActionResult> ReplaceBatch([FromBody] ImageBatchReplaceRequestModel model, CancellationToken cancellationToken)
    {
        if (!await AuthorizedAsync()) return AccessDeniedView();
        if (model?.Items is null || model.Items.Count == 0)
            return BadRequest(new { reasonCode = "image.batch.empty" });

        var items = model.Items.Select(item => new BatchImageReplacementItem
        {
            Sku = item.Sku,
            SourceUrl = item.SourceUrl,
            SeoName = item.SeoName,
            AltTextEn = item.AltTextEn,
            AltTextAr = item.AltTextAr
        }).ToList();

        var result = await _batchService.ReplaceBySkuAsync(items, actor: "admin", cancellationToken);
        return Json(result);
    }
}
