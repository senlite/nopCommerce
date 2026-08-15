using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Nop.Services.Configuration;
using Nop.Web.Framework;
using Nop.Web.Framework.Controllers;
using Nop.Web.Framework.Mvc.Filters;
using TwinParticles.CheckEngine.Application.Ai;
using TwinParticles.CheckEngine.Configuration;
using TwinParticles.CheckEngine.Domain.Ai;
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
    private readonly ISettingService _settingService;
    private readonly Nop.Services.Security.IPermissionService _permissionService;

    public AiAdminController(
        IAiUsageLedger usageLedger,
        IAiDataDisclosureCatalog disclosureCatalog,
        IAiSpendPolicy spendPolicy,
        AiContentCandidateService contentCandidateService,
        AiDisclosureService disclosureService,
        IAiGenerationRepository generationRepository,
        ISettingService settingService,
        Nop.Services.Security.IPermissionService permissionService)
    {
        _usageLedger = usageLedger;
        _disclosureCatalog = disclosureCatalog;
        _spendPolicy = spendPolicy;
        _contentCandidateService = contentCandidateService;
        _disclosureService = disclosureService;
        _generationRepository = generationRepository;
        _settingService = settingService;
        _permissionService = permissionService;
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

        var usage = await _usageLedger.GetDailyUsageAsync(featureKey, cancellationToken);
        var ceiling = _spendPolicy.ResolveDailyCeiling(featureKey);

        return Json(new
        {
            featureKey,
            dailyUsage = usage,
            dailyCeiling = ceiling,
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
            var daily = await _usageLedger.GetDailyUsageAsync(featureKey, cancellationToken);
            usage.Add(new
            {
                featureKey,
                dailyUsage = daily,
                dailyCeiling = _spendPolicy.ResolveDailyCeiling(featureKey)
            });
        }

        var pendingContent = await _generationRepository.GetPendingQueueAsync(200, cancellationToken);

        return Json(new
        {
            disclosureAcknowledged = _spendPolicy.DisclosureAcknowledged,
            usage,
            pendingContentCount = pendingContent.Count
        });
    }
}
