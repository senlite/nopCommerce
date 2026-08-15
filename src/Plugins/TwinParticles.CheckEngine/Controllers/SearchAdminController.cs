using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Nop.Web.Framework;
using Nop.Web.Framework.Controllers;
using Nop.Web.Framework.Mvc.Filters;
using TwinParticles.CheckEngine.Application.Search;
using TwinParticles.CheckEngine.Domain.Search;
using TwinParticles.CheckEngine.Security;

namespace TwinParticles.CheckEngine.Controllers;

[AuthorizeAdmin]
[Area(AreaNames.ADMIN)]
[AutoValidateAntiforgeryToken]
public sealed class SearchAdminController : BasePluginController
{
    private static readonly string[] DefaultEmbeddingLocales = ["en", "ar"];

    private readonly Nop.Services.Security.IPermissionService _permissionService;
    private readonly SearchIndexAdminService _service;
    private readonly SearchEmbeddingIndexBuilderService _embeddingIndexBuilder;
    private readonly SearchAnalyticsAdminService _analyticsService;
    private readonly ISearchIndexStateReader _indexStateReader;
    private readonly ISearchEmbeddingIndex _embeddingIndex;

    public SearchAdminController(
        SearchIndexAdminService service,
        SearchEmbeddingIndexBuilderService embeddingIndexBuilder,
        SearchAnalyticsAdminService analyticsService,
        ISearchIndexStateReader indexStateReader,
        ISearchEmbeddingIndex embeddingIndex,
        Nop.Services.Security.IPermissionService permissionService)
    {
        _service = service;
        _embeddingIndexBuilder = embeddingIndexBuilder;
        _analyticsService = analyticsService;
        _indexStateReader = indexStateReader;
        _embeddingIndex = embeddingIndex;
        _permissionService = permissionService;
    }

    private async Task<bool> AuthorizedAsync() => await _permissionService.AuthorizeAsync(CheckEnginePermissionProvider.ManageCheckEngine.SystemName);

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        if (!await AuthorizedAsync()) return AccessDeniedView();
        return View("~/Plugins/TwinParticles.CheckEngine/Views/Admin/SearchAdmin.cshtml");
    }

    [HttpGet]
    public async Task<IActionResult> Status(CancellationToken cancellationToken)
    {
        if (!await AuthorizedAsync()) return AccessDeniedView();

        var keywordState = await _indexStateReader.GetStateAsync(cancellationToken);
        var embeddings = new List<object>();

        foreach (var locale in DefaultEmbeddingLocales)
        {
            embeddings.Add(new
            {
                locale,
                ready = await _embeddingIndex.IsReadyAsync(locale, cancellationToken),
                count = await _embeddingIndex.GetCountAsync(locale, cancellationToken)
            });
        }

        return Json(new { keywordState, embeddings });
    }

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
        var result = await _embeddingIndexBuilder.RebuildAsync(locale, cancellationToken);
        return Json(result);
    }

    [HttpPost]
    public async Task<IActionResult> RebuildEmbeddingsAll(CancellationToken cancellationToken = default)
    {
        if (!await AuthorizedAsync()) return AccessDeniedView();

        var locales = new List<SearchEmbeddingRebuildResult>();
        var totalIndexed = 0;

        foreach (var locale in DefaultEmbeddingLocales)
        {
            var result = await _embeddingIndexBuilder.RebuildAsync(locale, cancellationToken);
            locales.Add(result);
            totalIndexed += result.Indexed;
        }

        return Json(new { locales, totalIndexed });
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
