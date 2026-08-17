using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Nop.Web.Framework;
using Nop.Web.Framework.Controllers;
using Nop.Web.Framework.Mvc.Filters;
using TwinParticles.CheckEngine.Application.Licensing;
using TwinParticles.CheckEngine.Application.Marketplace;
using TwinParticles.CheckEngine.Domain.Marketplace;
using TwinParticles.CheckEngine.Models;
using TwinParticles.CheckEngine.Security;

namespace TwinParticles.CheckEngine.Controllers;

[AuthorizeAdmin]
[Area(AreaNames.ADMIN)]
[AutoValidateAntiforgeryToken]
public sealed class VendorAdminController : BasePluginController
{
    private readonly VendorOnboardingService _onboardingService;
    private readonly MarketplaceUpgradeService _upgradeService;
    private readonly VendorIsolationService _isolationService;
    private readonly VendorDashboardService _dashboardService;
    private readonly MarketplaceLicenceGate _marketplaceGate;
    private readonly Nop.Services.Security.IPermissionService _permissionService;

    public VendorAdminController(
        VendorOnboardingService onboardingService,
        MarketplaceUpgradeService upgradeService,
        VendorIsolationService isolationService,
        VendorDashboardService dashboardService,
        MarketplaceLicenceGate marketplaceGate,
        Nop.Services.Security.IPermissionService permissionService)
    {
        _onboardingService = onboardingService;
        _upgradeService = upgradeService;
        _isolationService = isolationService;
        _dashboardService = dashboardService;
        _marketplaceGate = marketplaceGate;
        _permissionService = permissionService;
    }

    private async Task<bool> AuthorizedAsync()
        => await _permissionService.AuthorizeAsync(CheckEnginePermissionProvider.ManageCheckEngine.SystemName);

    [HttpGet]
    public async Task<IActionResult> ReviewBoard(CancellationToken cancellationToken)
    {
        if (!await AuthorizedAsync())
            return AccessDeniedView();
        if (!await _marketplaceGate.AllowsMarketplaceAsync(cancellationToken))
            return AccessDeniedView();

        return View("~/Plugins/TwinParticles.CheckEngine/Views/Admin/VendorReview.cshtml");
    }

    [HttpGet]
    public async Task<IActionResult> Queue(CancellationToken cancellationToken)
    {
        if (!await AuthorizedAsync())
            return AccessDeniedView();
        if (!await _marketplaceGate.AllowsMarketplaceAsync(cancellationToken))
            return Denied(VendorErrorCodes.LicenceDenied, 403);

        return Json(await _onboardingService.GetReviewQueueAsync(cancellationToken));
    }

    [HttpGet]
    public async Task<IActionResult> Get(int vendorId, CancellationToken cancellationToken)
    {
        if (!await AuthorizedAsync())
            return AccessDeniedView();
        if (!await _marketplaceGate.AllowsMarketplaceAsync(cancellationToken))
            return Denied(VendorErrorCodes.LicenceDenied, 403);

        var snapshot = await _onboardingService.GetSnapshotAsync(vendorId, cancellationToken);
        return snapshot is null ? NotFound() : Json(snapshot);
    }

    [HttpPost]
    public Task<IActionResult> Submit([FromBody] VendorReviewActionModel model, CancellationToken cancellationToken)
        => MutateAsync(model, (id, notes, ct) => _onboardingService.SubmitForReviewAsync(id, "admin", ct), cancellationToken);

    [HttpPost]
    public Task<IActionResult> Approve([FromBody] VendorReviewActionModel model, CancellationToken cancellationToken)
        => MutateAsync(model, (id, notes, ct) => _onboardingService.ApproveAsync(id, "admin", ct), cancellationToken);

    [HttpPost]
    public Task<IActionResult> Reject([FromBody] VendorReviewActionModel model, CancellationToken cancellationToken)
        => MutateAsync(model, (id, notes, ct) => _onboardingService.RejectAsync(id, "admin", notes, ct), cancellationToken);

    [HttpPost]
    public Task<IActionResult> Suspend([FromBody] VendorReviewActionModel model, CancellationToken cancellationToken)
        => MutateAsync(model, (id, notes, ct) => _onboardingService.SuspendAsync(id, "admin", notes, ct), cancellationToken);

    [HttpPost]
    public Task<IActionResult> Reinstate([FromBody] VendorReviewActionModel model, CancellationToken cancellationToken)
        => MutateAsync(model, (id, notes, ct) => _onboardingService.ReinstateAsync(id, "admin", ct), cancellationToken);

    [HttpPost]
    public Task<IActionResult> Close([FromBody] VendorReviewActionModel model, CancellationToken cancellationToken)
        => MutateAsync(model, (id, notes, ct) => _onboardingService.CloseAsync(id, "admin", notes, ct), cancellationToken);

    [HttpPost]
    public async Task<IActionResult> EnableMarketplace(CancellationToken cancellationToken)
    {
        if (!await AuthorizedAsync())
            return AccessDeniedView();

        var result = await _upgradeService.EnableAsync(cancellationToken);
        return result.Succeeded
            ? Json(result)
            : Denied(result.ReasonCode ?? VendorErrorCodes.LicenceDenied, 403);
    }

    [HttpGet]
    public async Task<IActionResult> Scoreboard(CancellationToken cancellationToken)
    {
        if (!await AuthorizedAsync())
            return AccessDeniedView();
        if (!await _marketplaceGate.AllowsMarketplaceAsync(cancellationToken))
            return AccessDeniedView();

        return View("~/Plugins/TwinParticles.CheckEngine/Views/Admin/VendorScoreboard.cshtml");
    }

    [HttpGet]
    public async Task<IActionResult> Scorecards(CancellationToken cancellationToken)
    {
        if (!await AuthorizedAsync())
            return AccessDeniedView();
        if (!await _marketplaceGate.AllowsMarketplaceAsync(cancellationToken))
            return Denied(VendorErrorCodes.LicenceDenied, 403);

        return Json(await _dashboardService.ListOperatorScorecardsAsync(VendorActor.OperatorAdmin, cancellationToken));
    }

    [HttpPost]
    public async Task<IActionResult> AssignProduct([FromBody] VendorAssignProductModel model, CancellationToken cancellationToken)
    {
        if (!await AuthorizedAsync())
            return AccessDeniedView();
        if (!await _marketplaceGate.AllowsMarketplaceAsync(cancellationToken))
            return Denied(VendorErrorCodes.LicenceDenied, 403);

        await _isolationService.AssignProductAsync(model.VendorId, model.ProductId, cancellationToken);
        return Json(new { model.VendorId, model.ProductId });
    }

    private async Task<IActionResult> MutateAsync(
        VendorReviewActionModel model,
        System.Func<int, string?, CancellationToken, Task<VendorOnboardingResult>> action,
        CancellationToken cancellationToken)
    {
        if (!await AuthorizedAsync())
            return AccessDeniedView();
        if (!await _marketplaceGate.AllowsMarketplaceAsync(cancellationToken))
            return Denied(VendorErrorCodes.LicenceDenied, 403);

        var result = await action(model.VendorId, model.Notes, cancellationToken);
        return result.Succeeded
            ? Json(result.Snapshot)
            : Denied(result.ReasonCode ?? VendorErrorCodes.IllegalTransition, 400);
    }

    private static JsonResult Denied(string reasonCode, int statusCode)
        => new(new { reasonCode }) { StatusCode = statusCode };
}
