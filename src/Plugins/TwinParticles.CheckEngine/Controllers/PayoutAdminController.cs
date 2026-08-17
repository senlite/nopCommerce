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
public sealed class PayoutAdminController : BasePluginController
{
    private readonly PayoutStatementService _payoutService;
    private readonly MarketplaceLicenceGate _marketplaceGate;
    private readonly Nop.Services.Security.IPermissionService _permissionService;

    public PayoutAdminController(
        PayoutStatementService payoutService,
        MarketplaceLicenceGate marketplaceGate,
        Nop.Services.Security.IPermissionService permissionService)
    {
        _payoutService = payoutService;
        _marketplaceGate = marketplaceGate;
        _permissionService = permissionService;
    }

    private async Task<bool> AuthorizedAsync()
        => await _permissionService.AuthorizeAsync(CheckEnginePermissionProvider.ManageCheckEngine.SystemName);

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        if (!await AuthorizedAsync())
            return AccessDeniedView();
        if (!await _marketplaceGate.AllowsMarketplaceAsync(cancellationToken))
            return AccessDeniedView();

        return View("~/Plugins/TwinParticles.CheckEngine/Views/Admin/PayoutAdmin.cshtml");
    }

    [HttpGet]
    public async Task<IActionResult> List(int vendorId, CancellationToken cancellationToken)
    {
        if (!await AuthorizedAsync())
            return AccessDeniedView();
        if (!await _marketplaceGate.AllowsMarketplaceAsync(cancellationToken))
            return Denied(VendorErrorCodes.LicenceDenied, 403);

        return Json(await _payoutService.ListAsync(vendorId, cancellationToken));
    }

    [HttpGet]
    public async Task<IActionResult> Get(int statementId, CancellationToken cancellationToken)
    {
        if (!await AuthorizedAsync())
            return AccessDeniedView();
        if (!await _marketplaceGate.AllowsMarketplaceAsync(cancellationToken))
            return Denied(VendorErrorCodes.LicenceDenied, 403);

        var statement = await _payoutService.GetAsync(statementId, cancellationToken);
        return statement is null ? NotFound() : Json(statement);
    }

    [HttpPost]
    public async Task<IActionResult> Generate([FromBody] PayoutGenerateModel model, CancellationToken cancellationToken)
    {
        if (!await AuthorizedAsync())
            return AccessDeniedView();
        if (!await _marketplaceGate.AllowsMarketplaceAsync(cancellationToken))
            return Denied(VendorErrorCodes.LicenceDenied, 403);

        var statement = await _payoutService.GenerateAsync(
            model.VendorId, model.PeriodStartUtc, model.PeriodEndUtc, cancellationToken);
        return statement is null ? Denied(VendorErrorCodes.LicenceDenied, 403) : Json(statement);
    }

    [HttpPost]
    public async Task<IActionResult> Finalize([FromBody] PayoutStatementActionModel model, CancellationToken cancellationToken)
    {
        if (!await AuthorizedAsync())
            return AccessDeniedView();

        var statement = await _payoutService.FinalizeAsync(model.StatementId, cancellationToken);
        return statement is null ? NotFound() : Json(statement);
    }

    [HttpPost]
    public async Task<IActionResult> PushErp([FromBody] PayoutStatementActionModel model, CancellationToken cancellationToken)
    {
        if (!await AuthorizedAsync())
            return AccessDeniedView();

        var statement = await _payoutService.PushToErpAsync(model.StatementId, cancellationToken);
        return statement is null ? NotFound() : Json(statement);
    }

    [HttpPost]
    public async Task<IActionResult> Reconcile([FromBody] PayoutStatementActionModel model, CancellationToken cancellationToken)
    {
        if (!await AuthorizedAsync())
            return AccessDeniedView();

        var result = await _payoutService.ReconcileAsync(model.StatementId, cancellationToken);
        return result is null ? NotFound() : Json(result);
    }

    [HttpPost]
    public async Task<IActionResult> Adjust([FromBody] PayoutAdjustmentModel model, CancellationToken cancellationToken)
    {
        if (!await AuthorizedAsync())
            return AccessDeniedView();

        var adjustment = await _payoutService.AddAdjustmentAsync(
            model.VendorId, model.Amount, model.ReasonCode, model.Notes, "admin", cancellationToken);
        return adjustment is null ? Denied(VendorErrorCodes.LicenceDenied, 403) : Json(adjustment);
    }

    private static JsonResult Denied(string reasonCode, int statusCode)
        => new(new { reasonCode }) { StatusCode = statusCode };
}
