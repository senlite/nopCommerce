using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Nop.Services.Configuration;
using Nop.Web.Framework;
using Nop.Web.Framework.Controllers;
using Nop.Web.Framework.Mvc.Filters;
using TwinParticles.CheckEngine.Application.Search;
using TwinParticles.CheckEngine.Configuration;
using TwinParticles.CheckEngine.Domain.Security;
using TwinParticles.CheckEngine.Domain.Search;
using TwinParticles.CheckEngine.Models;
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
    private readonly ISearchEmbeddingCatalogSource _catalogSource;
    private readonly BilingualSearchSynonymService _synonymService;
    private readonly ISettingService _settingService;
    private readonly ICheckEngineAuditService _auditService;

    public SearchAdminController(
        SearchIndexAdminService service,
        SearchEmbeddingIndexBuilderService embeddingIndexBuilder,
        SearchAnalyticsAdminService analyticsService,
        ISearchIndexStateReader indexStateReader,
        ISearchEmbeddingIndex embeddingIndex,
        ISearchEmbeddingCatalogSource catalogSource,
        BilingualSearchSynonymService synonymService,
        ISettingService settingService,
        ICheckEngineAuditService auditService,
        Nop.Services.Security.IPermissionService permissionService)
    {
        _service = service;
        _embeddingIndexBuilder = embeddingIndexBuilder;
        _analyticsService = analyticsService;
        _indexStateReader = indexStateReader;
        _embeddingIndex = embeddingIndex;
        _catalogSource = catalogSource;
        _synonymService = synonymService;
        _settingService = settingService;
        _auditService = auditService;
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
            var count = await _embeddingIndex.GetCountAsync(locale, cancellationToken);
            var catalogCount = await _catalogSource.GetCatalogCountAsync(locale, cancellationToken);
            var staleOptions = await _embeddingIndexBuilder.CreateStaleOptionsAsync(cancellationToken);
            var staleCount = await _catalogSource.GetStaleCountAsync(locale, staleOptions, cancellationToken);
            embeddings.Add(new
            {
                locale,
                ready = count > 0,
                count,
                catalogCount,
                staleCount,
                semanticReady = count > 0 && staleCount == 0 && count >= catalogCount
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
    public async Task<IActionResult> RebuildKeywordAndRefreshEmbeddings(CancellationToken cancellationToken = default)
    {
        if (!await AuthorizedAsync()) return AccessDeniedView();

        await _service.RebuildAsync(cancellationToken);

        var locales = new List<SearchEmbeddingRebuildResult>();
        var totalIndexed = 0;

        foreach (var locale in DefaultEmbeddingLocales)
        {
            var result = await _embeddingIndexBuilder.RefreshIncrementalAsync(locale, cancellationToken);
            locales.Add(result);
            totalIndexed += result.Indexed;
        }

        return Json(new { keywordRebuilt = true, locales, totalIndexed });
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

    [HttpPost]
    public async Task<IActionResult> RefreshEmbeddingsIncremental(string locale = "en", CancellationToken cancellationToken = default)
    {
        if (!await AuthorizedAsync()) return AccessDeniedView();
        var result = await _embeddingIndexBuilder.RefreshIncrementalAsync(locale, cancellationToken);
        return Json(result);
    }

    [HttpPost]
    public async Task<IActionResult> RefreshEmbeddingsIncrementalAll(CancellationToken cancellationToken = default)
    {
        if (!await AuthorizedAsync()) return AccessDeniedView();

        var locales = new List<SearchEmbeddingRebuildResult>();
        var totalIndexed = 0;

        foreach (var locale in DefaultEmbeddingLocales)
        {
            var result = await _embeddingIndexBuilder.RefreshIncrementalAsync(locale, cancellationToken);
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

    [HttpGet]
    public async Task<IActionResult> SynonymsData(CancellationToken cancellationToken)
    {
        if (!await AuthorizedAsync()) return AccessDeniedView();

        var settings = await _settingService.LoadSettingAsync<CheckEnginePluginSettings>();
        var overrides = SearchSynonymOverridesJson.Parse(settings.SearchSynonymOverridesJson);
        var merged = _synonymService.GetArabicToEnglishPairs();

        var rows = merged
            .OrderBy(pair => pair.Key, StringComparer.OrdinalIgnoreCase)
            .Select(pair => new
            {
                arabic = pair.Key,
                english = pair.Value,
                source = overrides.ContainsKey(pair.Key) ? "override" : "embedded"
            });

        return Json(new
        {
            embeddedCount = rows.Count(row => row.source == "embedded"),
            overrideCount = overrides.Count,
            terms = rows
        });
    }

    [HttpPost]
    public async Task<IActionResult> SaveSynonyms([FromBody] SearchSynonymsSaveModel model, CancellationToken cancellationToken)
    {
        if (!await AuthorizedAsync()) return AccessDeniedView();
        if (model?.Overrides is null)
            return BadRequest();

        var settings = await _settingService.LoadSettingAsync<CheckEnginePluginSettings>();
        var before = settings.SearchSynonymOverridesJson;
        settings.SearchSynonymOverridesJson = SearchSynonymOverridesJson.Serialize(model.Overrides);
        await _settingService.SaveSettingAsync(settings);

        await _auditService.AppendAsync(
            "admin",
            "search.synonyms_saved",
            "SearchSynonyms",
            "global",
            before,
            settings.SearchSynonymOverridesJson,
            cancellationToken);

        return Json(new { saved = true, overrideCount = model.Overrides.Count });
    }
}
