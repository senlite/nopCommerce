using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Nop.Web.Framework;
using Nop.Web.Framework.Controllers;
using Nop.Web.Framework.Mvc.Filters;
using TwinParticles.CheckEngine.Application.Search;
using TwinParticles.CheckEngine.Security;

namespace TwinParticles.CheckEngine.Controllers;

[AuthorizeAdmin]
[Area(AreaNames.ADMIN)]
[AutoValidateAntiforgeryToken]
public sealed class SearchAdminController : BasePluginController
{
    private readonly Nop.Services.Security.IPermissionService _permissionService;
    private readonly SearchIndexAdminService _service;
    private readonly SearchEmbeddingIndexBuilderService _embeddingIndexBuilder;
    private readonly SearchAnalyticsAdminService _analyticsService;

    public SearchAdminController(
        SearchIndexAdminService service,
        SearchEmbeddingIndexBuilderService embeddingIndexBuilder,
        SearchAnalyticsAdminService analyticsService,
        Nop.Services.Security.IPermissionService permissionService)
    {
        _service = service;
        _embeddingIndexBuilder = embeddingIndexBuilder;
        _analyticsService = analyticsService;
        _permissionService = permissionService;
    }

    private async Task<bool> AuthorizedAsync() => await _permissionService.AuthorizeAsync(CheckEnginePermissionProvider.ManageCheckEngine.SystemName);

    [HttpPost]
    public async Task<IActionResult> Rebuild(CancellationToken cancellationToken)
    {
        if (!await AuthorizedAsync()) return AccessDeniedView();

        await _service.RebuildAsync(cancellationToken);
        return Ok();
    }

    [HttpPost]
    public async Task<IActionResult> RebuildEmbeddings(string locale = "en", CancellationToken cancellationToken = default)
    {
        if (!await AuthorizedAsync()) return AccessDeniedView();
        var indexed = await _embeddingIndexBuilder.RebuildAsync(locale, cancellationToken);
        return Json(new { indexed, locale });
    }

    [HttpGet]
    public async Task<IActionResult> Analytics(int days = 30, CancellationToken cancellationToken = default)
    {
        if (!await AuthorizedAsync()) return AccessDeniedView();
        return Json(await _analyticsService.GetSummaryAsync(days, cancellationToken));
    }

    [HttpPost]
    public async Task<IActionResult> PruneAnalytics(int retentionDays = 90, CancellationToken cancellationToken = default)
    {
        if (!await AuthorizedAsync()) return AccessDeniedView();
        var deleted = await _analyticsService.PruneAsync(retentionDays, cancellationToken);
        return Json(new { deleted });
    }
}
