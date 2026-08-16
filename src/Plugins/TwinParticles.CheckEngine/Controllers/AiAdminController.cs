using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Nop.Services.Configuration;
using Nop.Web.Framework;
using Nop.Web.Framework.Controllers;
using Nop.Web.Framework.Mvc.Filters;
using TwinParticles.CheckEngine.Application.Ai;
using TwinParticles.CheckEngine.Application.Fitment;
using TwinParticles.CheckEngine.Configuration;
using TwinParticles.CheckEngine.Application.L10n;
using TwinParticles.CheckEngine.Domain.Ai;
using TwinParticles.CheckEngine.Domain.Security;
using TwinParticles.CheckEngine.L10n;
using TwinParticles.CheckEngine.Models;
using TwinParticles.CheckEngine.Security;

namespace TwinParticles.CheckEngine.Controllers;

[AuthorizeAdmin]
[Area(AreaNames.ADMIN)]
[AutoValidateAntiforgeryToken]
public sealed class AiAdminController : BasePluginController
{
    private readonly IAiUsageLedger _usageLedger;
    private readonly IAiDataDisclosureCatalog _disclosureCatalog;
    private readonly IAiSpendPolicy _spendPolicy;
    private readonly AiContentCandidateService _contentCandidateService;
    private readonly AiDisclosureService _disclosureService;
    private readonly IAiGenerationRepository _generationRepository;
    private readonly FitmentReviewService _fitmentReviewService;
    private readonly ISettingService _settingService;
    private readonly Nop.Services.Security.IPermissionService _permissionService;
    private readonly AiSpendAlertService _spendAlertService;
    private readonly AutomotiveGlossaryService _glossaryService;
    private readonly ICheckEngineAuditService _auditService;

    public AiAdminController(
        IAiUsageLedger usageLedger,
        IAiDataDisclosureCatalog disclosureCatalog,
        IAiSpendPolicy spendPolicy,
        AiContentCandidateService contentCandidateService,
        AiDisclosureService disclosureService,
        IAiGenerationRepository generationRepository,
        FitmentReviewService fitmentReviewService,
        ISettingService settingService,
        Nop.Services.Security.IPermissionService permissionService,
        AiSpendAlertService spendAlertService,
        AutomotiveGlossaryService glossaryService,
        ICheckEngineAuditService auditService)
    {
        _usageLedger = usageLedger;
        _disclosureCatalog = disclosureCatalog;
        _spendPolicy = spendPolicy;
        _contentCandidateService = contentCandidateService;
        _disclosureService = disclosureService;
        _generationRepository = generationRepository;
        _fitmentReviewService = fitmentReviewService;
        _settingService = settingService;
        _permissionService = permissionService;
        _spendAlertService = spendAlertService;
        _glossaryService = glossaryService;
        _auditService = auditService;
    }

    private async Task<bool> AuthorizedAsync() =>
        await _permissionService.AuthorizeAsync(CheckEnginePermissionProvider.ManageCheckEngine.SystemName);

    [HttpGet]
    public async Task<IActionResult> Disclosure(string featureKey, CancellationToken cancellationToken)
    {
        if (!await AuthorizedAsync()) return AccessDeniedView();
        return Json(_disclosureCatalog.GetItemsForFeature(featureKey));
    }

    [HttpGet]
    public async Task<IActionResult> Usage(string featureKey, CancellationToken cancellationToken)
    {
        if (!await AuthorizedAsync()) return AccessDeniedView();

        var summary = await _usageLedger.GetUsageSummaryAsync(featureKey, cancellationToken);
        var ceiling = _spendPolicy.ResolveDailyCeiling(featureKey);

        return Json(new
        {
            featureKey,
            dailyUsage = summary.TodayTokens,
            dailyCeiling = ceiling,
            summary,
            disclosureAcknowledged = _spendPolicy.DisclosureAcknowledged
        });
    }

    [HttpGet]
    public async Task<IActionResult> Candidates(
        AiGenerationEntityType entityType,
        int entityId,
        CancellationToken cancellationToken)
    {
        if (!await AuthorizedAsync()) return AccessDeniedView();
        return Json(await _contentCandidateService.GetPendingAsync(entityType, entityId, cancellationToken));
    }

    [HttpPost]
    public async Task<IActionResult> AcknowledgeDisclosure(CancellationToken cancellationToken)
    {
        if (!await AuthorizedAsync()) return AccessDeniedView();

        var acknowledged = _disclosureService.Acknowledge();
        var settings = await _settingService.LoadSettingAsync<CheckEnginePluginSettings>();
        settings.AiDisclosureAcknowledged = acknowledged;
        await _settingService.SaveSettingAsync(settings);
        CheckEngineAiSettingsSync.Apply(settings);

        return Json(new { acknowledged });
    }

    [HttpGet]
    public async Task<IActionResult> Queue(int take = 50, CancellationToken cancellationToken = default)
    {
        if (!await AuthorizedAsync()) return AccessDeniedView();
        return Json(await _contentCandidateService.GetPendingQueueAsync(take, cancellationToken));
    }

    [HttpPost]
    public async Task<IActionResult> Review([FromBody] AiCandidateReviewModel model, CancellationToken cancellationToken)
    {
        if (!await AuthorizedAsync()) return AccessDeniedView();
        if (model is null || model.Id <= 0)
            return BadRequest();

        var ok = await _contentCandidateService.ReviewAsync(model.Id, model.Approved, "admin", cancellationToken);
        return ok ? Ok() : NotFound();
    }

    [HttpGet]
    public async Task<IActionResult> ReviewBoard(CancellationToken cancellationToken)
    {
        if (!await AuthorizedAsync()) return AccessDeniedView();
        return View("~/Plugins/TwinParticles.CheckEngine/Views/Admin/AiReview.cshtml");
    }

    [HttpGet]
    public async Task<IActionResult> Dashboard(CancellationToken cancellationToken)
    {
        if (!await AuthorizedAsync()) return AccessDeniedView();
        return View("~/Plugins/TwinParticles.CheckEngine/Views/Admin/AiDashboard.cshtml");
    }

    [HttpGet]
    public async Task<IActionResult> DashboardData(CancellationToken cancellationToken)
    {
        if (!await AuthorizedAsync()) return AccessDeniedView();

        var features = new[]
        {
            AiFeatureKeys.ImportEnrichment,
            AiFeatureKeys.ImportTranslation,
            AiFeatureKeys.ImportSeo,
            AiFeatureKeys.SearchNaturalLanguage,
            AiFeatureKeys.SearchSemantic,
            AiFeatureKeys.FitmentInference,
            AiFeatureKeys.CustomerAssistant
        };

        var usage = new List<object>();
        foreach (var featureKey in features)
        {
            var summary = await _usageLedger.GetUsageSummaryAsync(featureKey, cancellationToken);
            var ceiling = _spendPolicy.ResolveDailyCeiling(featureKey);
            usage.Add(new
            {
                featureKey,
                dailyUsage = summary.TodayTokens,
                dailyCeiling = ceiling,
                summary,
                todayEstimatedCostUsd = summary.TodayEstimatedCostUsd,
                last7DaysEstimatedCostUsd = summary.Last7DaysEstimatedCostUsd,
                last30DaysEstimatedCostUsd = summary.Last30DaysEstimatedCostUsd,
                todayFailureRate = AiUsageSummary.FailureRate(summary.TodayAttempts, summary.TodayFailures),
                last7DaysFailureRate = AiUsageSummary.FailureRate(summary.Last7DaysAttempts, summary.Last7DaysFailures),
                last30DaysFailureRate = AiUsageSummary.FailureRate(summary.Last30DaysAttempts, summary.Last30DaysFailures)
            });
        }

        var pendingContent = await _generationRepository.GetPendingQueueAsync(200, cancellationToken);
        var pendingFitmentAi = await _fitmentReviewService.GetAiQueueAsync(cancellationToken);
        var globalUsage = await _usageLedger.GetGlobalDailyUsageAsync(cancellationToken);

        return Json(new
        {
            disclosureAcknowledged = _spendPolicy.DisclosureAcknowledged,
            globalDailyUsage = globalUsage,
            globalDailyCeiling = _spendPolicy.GlobalDailyCeiling,
            tokenCostPer1KUsd = _spendPolicy.TokenCostPer1KUsd,
            recentBudgetAlerts = _spendAlertService.GetRecentAlerts(),
            usage,
            pendingContentCount = pendingContent.Count,
            pendingFitmentAiCount = pendingFitmentAi.Count
        });
    }

    [HttpGet]
    public async Task<IActionResult> GlossaryBoard(CancellationToken cancellationToken)
    {
        if (!await AuthorizedAsync()) return AccessDeniedView();
        return View("~/Plugins/TwinParticles.CheckEngine/Views/Admin/GlossaryAdmin.cshtml");
    }

    [HttpGet]
    public async Task<IActionResult> GlossaryData(CancellationToken cancellationToken)
    {
        if (!await AuthorizedAsync()) return AccessDeniedView();

        var settings = await _settingService.LoadSettingAsync<CheckEnginePluginSettings>();
        var overrides = PluginAutomotiveGlossaryOverridesSource.ParseOverrides(settings.AutomotiveGlossaryOverridesJson);
        var embedded = new AutomotiveGlossaryService().GetTerms();
        var merged = _glossaryService.GetTerms();

        var rows = merged
            .OrderBy(pair => pair.Key, StringComparer.OrdinalIgnoreCase)
            .Select(pair => new
            {
                english = pair.Key,
                arabic = pair.Value,
                source = overrides.ContainsKey(pair.Key) ? "override" : "embedded"
            });

        return Json(new
        {
            embeddedCount = embedded.Count,
            overrideCount = overrides.Count,
            terms = rows
        });
    }

    [HttpPost]
    public async Task<IActionResult> SaveGlossary([FromBody] GlossarySaveModel model, CancellationToken cancellationToken)
    {
        if (!await AuthorizedAsync()) return AccessDeniedView();
        if (model?.Overrides is null)
            return BadRequest();

        var settings = await _settingService.LoadSettingAsync<CheckEnginePluginSettings>();
        var before = settings.AutomotiveGlossaryOverridesJson;
        settings.AutomotiveGlossaryOverridesJson = PluginAutomotiveGlossaryOverridesSource.SerializeOverrides(model.Overrides);
        await _settingService.SaveSettingAsync(settings);

        await _auditService.AppendAsync(
            "admin",
            "glossary.overrides_saved",
            "AutomotiveGlossary",
            "global",
            before,
            settings.AutomotiveGlossaryOverridesJson,
            cancellationToken);

        return Json(new { saved = true, overrideCount = model.Overrides.Count });
    }
}
