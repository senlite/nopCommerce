using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Nop.Web.Framework;
using Nop.Web.Framework.Controllers;
using Nop.Web.Framework.Mvc.Filters;
using TwinParticles.CheckEngine.Application.Licensing;
using TwinParticles.CheckEngine.Application.Marketplace;
using TwinParticles.CheckEngine.Domain.Marketplace;
using TwinParticles.CheckEngine.Infrastructure;
using TwinParticles.CheckEngine.Models;
using TwinParticles.CheckEngine.Security;

namespace TwinParticles.CheckEngine.Controllers;

[AuthorizeAdmin]
[Area(AreaNames.ADMIN)]
[AutoValidateAntiforgeryToken]
public sealed class CommissionAdminController : BasePluginController
{
    private readonly CommissionPlanAdminService _planAdmin;
    private readonly CommissionSnapshotService _snapshotService;
    private readonly MarketplaceLicenceGate _marketplaceGate;
    private readonly Nop.Services.Security.IPermissionService _permissionService;

    public CommissionAdminController(
        CommissionPlanAdminService planAdmin,
        CommissionSnapshotService snapshotService,
        MarketplaceLicenceGate marketplaceGate,
        Nop.Services.Security.IPermissionService permissionService)
    {
        _planAdmin = planAdmin;
        _snapshotService = snapshotService;
        _marketplaceGate = marketplaceGate;
        _permissionService = permissionService;
    }

    private async Task<bool> AuthorizedAsync()
        => await _permissionService.AuthorizeAsync(CheckEnginePermissionProvider.ManageCheckEngine.SystemName);

    [HttpGet]
    public async Task<IActionResult> Configure(int vendorId, CancellationToken cancellationToken)
    {
        if (!await AuthorizedAsync())
            return AccessDeniedView();
        if (!await _marketplaceGate.AllowsMarketplaceAsync(cancellationToken))
            return AccessDeniedView();

        ViewBag.VendorId = vendorId;
        return View("~/Plugins/TwinParticles.CheckEngine/Views/Admin/CommissionConfigure.cshtml");
    }

    [HttpGet]
    public async Task<IActionResult> GetPlan(int vendorId, CancellationToken cancellationToken)
    {
        if (!await AuthorizedAsync())
            return AccessDeniedView();
        if (!await _marketplaceGate.AllowsMarketplaceAsync(cancellationToken))
            return Denied(VendorErrorCodes.LicenceDenied, 403);

        if (!Request.WantsJsonResponse())
            return CheckEnginePaths.RedirectAdmin("CommissionAdmin", "Configure", $"vendorId={vendorId}");

        var plan = await _planAdmin.GetPlanAsync(vendorId, cancellationToken);
        return plan is null ? Json(new { vendorId, rules = Array.Empty<object>() }) : Json(plan);
    }

    [HttpPost]
    public async Task<IActionResult> SavePlan([FromBody] CommissionPlanModel model, CancellationToken cancellationToken)
    {
        if (!await AuthorizedAsync())
            return AccessDeniedView();
        if (!await _marketplaceGate.AllowsMarketplaceAsync(cancellationToken))
            return Denied(VendorErrorCodes.LicenceDenied, 403);

        var plan = Map(model);
        var saved = await _planAdmin.SavePlanAsync(plan, cancellationToken);
        return saved is null ? Denied(VendorErrorCodes.LicenceDenied, 403) : Json(saved);
    }

    [HttpGet]
    public async Task<IActionResult> OrderSnapshots(int orderId, CancellationToken cancellationToken)
    {
        if (!await AuthorizedAsync())
            return AccessDeniedView();
        if (!await _marketplaceGate.AllowsMarketplaceAsync(cancellationToken))
            return Denied(VendorErrorCodes.LicenceDenied, 403);

        if (!Request.WantsJsonResponse())
            return CheckEnginePaths.RedirectAdmin("VendorAdmin", "Scoreboard", $"orderId={orderId}");

        return Json(await _snapshotService.GetSnapshotsAsync(orderId, cancellationToken));
    }

    private static CommissionPlan Map(CommissionPlanModel model)
        => new()
        {
            Id = model.Id,
            VendorId = model.VendorId,
            Name = model.Name,
            IsActive = model.IsActive,
            Rules = model.Rules.Select(rule => new CommissionRule
            {
                Id = rule.Id,
                ModelKind = (CommissionModelKind)rule.ModelKind,
                Basis = (CommissionBasis)rule.Basis,
                Priority = rule.Priority,
                FlatAmount = rule.FlatAmount,
                PercentageRate = rule.PercentageRate,
                CategoryId = rule.CategoryId,
                EffectiveFromUtc = rule.EffectiveFromUtc,
                EffectiveToUtc = rule.EffectiveToUtc,
                IsActive = rule.IsActive,
                TierBands = rule.TierBands.Select(band => new CommissionTierBand
                {
                    MinVolume = band.MinVolume,
                    MaxVolume = band.MaxVolume,
                    PercentageRate = band.PercentageRate
                }).ToList()
            }).ToList()
        };

    private static JsonResult Denied(string reasonCode, int statusCode)
        => new(new { reasonCode }) { StatusCode = statusCode };
}
