using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Nop.Web.Framework;
using Nop.Web.Framework.Controllers;
using Nop.Web.Framework.Mvc.Filters;
using TwinParticles.CheckEngine.Application.Ai;
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
    private readonly Nop.Services.Security.IPermissionService _permissionService;

    public AiAdminController(
        IAiUsageLedger usageLedger,
        IAiDataDisclosureCatalog disclosureCatalog,
        IAiSpendPolicy spendPolicy,
        AiContentCandidateService contentCandidateService,
        AiDisclosureService disclosureService,
        Nop.Services.Security.IPermissionService permissionService)
    {
        _usageLedger = usageLedger;
        _disclosureCatalog = disclosureCatalog;
        _spendPolicy = spendPolicy;
        _contentCandidateService = contentCandidateService;
        _disclosureService = disclosureService;
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
        return Json(new { acknowledged = _disclosureService.Acknowledge() });
    }
}
