using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Nop.Web.Framework;
using Nop.Web.Framework.Controllers;
using Nop.Web.Framework.Mvc.Filters;
using Nop.Services.Configuration;
using TwinParticles.CheckEngine.Application.Images;
using TwinParticles.CheckEngine.Configuration;
using TwinParticles.CheckEngine.Infrastructure;
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
    private readonly SupplierImageSourcingService _supplierSourcingService;
    private readonly ISettingService _settingService;
    private readonly Nop.Services.Security.IPermissionService _permissionService;

    public ImageAdminController(
        ProductImageService service,
        BatchImageReplacementService batchService,
        SupplierImageSourcingService supplierSourcingService,
        ISettingService settingService,
        Nop.Services.Security.IPermissionService permissionService)
    {
        _service = service;
        _batchService = batchService;
        _supplierSourcingService = supplierSourcingService;
        _settingService = settingService;
        _permissionService = permissionService;
    }

    private async Task<bool> AuthorizedAsync() => await _permissionService.AuthorizeAsync(CheckEnginePermissionProvider.ManageCheckEngine.SystemName);

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        if (!await AuthorizedAsync()) return AccessDeniedView();
        var settings = await _settingService.LoadSettingAsync<CheckEnginePluginSettings>();
        ViewBag.SupplierImageUrlTemplate = settings.SupplierImageUrlTemplate ?? string.Empty;
        return View("~/Plugins/TwinParticles.CheckEngine/Views/Admin/ImageAdmin.cshtml");
    }

    [HttpGet]
    public async Task<IActionResult> Template(CancellationToken cancellationToken)
    {
        if (!await AuthorizedAsync()) return AccessDeniedView();
        if (!Request.WantsJsonResponse())
            return CheckEnginePaths.RedirectAdmin("ImageAdmin", "Index");

        var settings = await _settingService.LoadSettingAsync<CheckEnginePluginSettings>();
        return Json(new { urlTemplate = settings.SupplierImageUrlTemplate ?? string.Empty });
    }

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

    [HttpPost]
    public async Task<IActionResult> SourceFromManifest([FromBody] ImageSupplierManifestRequestModel model, CancellationToken cancellationToken)
    {
        if (!await AuthorizedAsync()) return AccessDeniedView();
        if (string.IsNullOrWhiteSpace(model?.CsvContent))
            return BadRequest(new { reasonCode = "image.supplier.manifest_empty" });

        var result = await _supplierSourcingService.ReplaceFromManifestCsvAsync(model.CsvContent, actor: "admin", cancellationToken);
        return Json(result);
    }

    [HttpPost]
    public async Task<IActionResult> SourceFromTemplate([FromBody] ImageSupplierTemplateRequestModel model, CancellationToken cancellationToken)
    {
        if (!await AuthorizedAsync()) return AccessDeniedView();
        if (model?.Skus is null || model.Skus.Count == 0)
            return BadRequest(new { reasonCode = "image.supplier.skus_empty" });

        var settings = await _settingService.LoadSettingAsync<CheckEnginePluginSettings>();
        var template = model.UrlTemplate?.Trim();
        if (!string.IsNullOrWhiteSpace(template))
        {
            settings.SupplierImageUrlTemplate = template;
            await _settingService.SaveSettingAsync(settings);
        }
        else
        {
            template = settings.SupplierImageUrlTemplate;
        }

        var result = await _supplierSourcingService.ReplaceFromUrlTemplateAsync(
            model.Skus,
            actor: "admin",
            cancellationToken,
            urlTemplateOverride: template);
        return Json(result);
    }
}
