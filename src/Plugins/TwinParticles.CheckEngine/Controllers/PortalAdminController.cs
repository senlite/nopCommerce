using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Nop.Services.Security;
using Nop.Web.Framework;
using Nop.Web.Framework.Controllers;
using Nop.Web.Framework.Mvc.Filters;
using TwinParticles.CheckEngine.Application.Portals;
using TwinParticles.CheckEngine.Security;

namespace TwinParticles.CheckEngine.Controllers;

[AuthorizeAdmin]
[Area(AreaNames.ADMIN)]
[AutoValidateAntiforgeryToken]
public sealed class PortalAdminController : BasePluginController
{
    private readonly PortalAdminService _portalAdmin;
    private readonly IPermissionService _permissionService;

    public PortalAdminController(PortalAdminService portalAdmin, IPermissionService permissionService)
    {
        _portalAdmin = portalAdmin;
        _permissionService = permissionService;
    }

    [HttpGet]
    public async Task<IActionResult> Accounts(CancellationToken cancellationToken)
    {
        if (!await AuthorizedAsync())
            return AccessDeniedView();

        return View("~/Plugins/TwinParticles.CheckEngine/Views/Admin/PortalAccounts.cshtml");
    }

    [HttpPost]
    public async Task<IActionResult> ProvisionWorkshop([FromBody] ProvisionAccountRequest request, CancellationToken cancellationToken)
    {
        if (!await AuthorizedAsync())
            return AccessDeniedData();

        var result = await _portalAdmin.ProvisionWorkshopAccountAsync(request, cancellationToken);
        return result.Success ? Json(result) : BadRequestJson(result);
    }

    [HttpPost]
    public async Task<IActionResult> ProvisionFleet([FromBody] ProvisionAccountRequest request, CancellationToken cancellationToken)
    {
        if (!await AuthorizedAsync())
            return AccessDeniedData();

        var result = await _portalAdmin.ProvisionFleetAccountAsync(request, cancellationToken);
        return result.Success ? Json(result) : BadRequestJson(result);
    }

    [HttpPost]
    public async Task<IActionResult> ProvisionDealer([FromBody] ProvisionAccountRequest request, CancellationToken cancellationToken)
    {
        if (!await AuthorizedAsync())
            return AccessDeniedData();

        var result = await _portalAdmin.ProvisionDealerAccountAsync(request, cancellationToken);
        return result.Success ? Json(result) : BadRequestJson(result);
    }

    [HttpPost]
    public async Task<IActionResult> SeedDealerAllocation([FromBody] SeedDealerAllocationRequest request, CancellationToken cancellationToken)
    {
        if (!await AuthorizedAsync())
            return AccessDeniedData();

        var result = await _portalAdmin.SeedDealerAllocationAsync(request, cancellationToken);
        return result.Success ? Json(result) : BadRequestJson(result);
    }

    [HttpPost]
    public async Task<IActionResult> SeedDealerQuota([FromBody] SeedDealerQuotaRequest request, CancellationToken cancellationToken)
    {
        if (!await AuthorizedAsync())
            return AccessDeniedData();

        var result = await _portalAdmin.SeedDealerQuotaAsync(request, cancellationToken);
        return result.Success ? Json(result) : BadRequestJson(result);
    }

    [HttpPost]
    public async Task<IActionResult> SeedDealerFranchise([FromBody] SeedDealerFranchiseRequest request, CancellationToken cancellationToken)
    {
        if (!await AuthorizedAsync())
            return AccessDeniedData();

        var result = await _portalAdmin.SeedDealerFranchiseAsync(request, cancellationToken);
        return result.Success ? Json(result) : BadRequestJson(result);
    }

    [HttpPost]
    public async Task<IActionResult> SeedTradePriceListItem([FromBody] SeedTradePriceListRequest request, CancellationToken cancellationToken)
    {
        if (!await AuthorizedAsync())
            return AccessDeniedData();

        var result = await _portalAdmin.SeedTradePriceListItemAsync(request, cancellationToken);
        return result.Success ? Json(result) : BadRequestJson(result);
    }

    [HttpPost]
    public async Task<IActionResult> SeedWorkshopTechnician([FromBody] SeedWorkshopTechnicianRequest request, CancellationToken cancellationToken)
    {
        if (!await AuthorizedAsync())
            return AccessDeniedData();

        var result = await _portalAdmin.SeedWorkshopTechnicianAsync(request, cancellationToken);
        return result.Success ? Json(result) : BadRequestJson(result);
    }

    [HttpPost]
    public async Task<IActionResult> SeedWorkshopLabourRate([FromBody] SeedWorkshopLabourRateRequest request, CancellationToken cancellationToken)
    {
        if (!await AuthorizedAsync())
            return AccessDeniedData();

        var result = await _portalAdmin.SeedWorkshopLabourRateAsync(request, cancellationToken);
        return result.Success ? Json(result) : BadRequestJson(result);
    }

    [HttpPost]
    public async Task<IActionResult> SeedFleetApprover([FromBody] SeedFleetApproverRequest request, CancellationToken cancellationToken)
    {
        if (!await AuthorizedAsync())
            return AccessDeniedData();

        var result = await _portalAdmin.SeedFleetApproverAsync(request, cancellationToken);
        return result.Success ? Json(result) : BadRequestJson(result);
    }

    [HttpPost]
    public async Task<IActionResult> SeedDealerTerritory([FromBody] SeedDealerTerritoryRequest request, CancellationToken cancellationToken)
    {
        if (!await AuthorizedAsync())
            return AccessDeniedData();

        var result = await _portalAdmin.SeedDealerTerritoryAsync(request, cancellationToken);
        return result.Success ? Json(result) : BadRequestJson(result);
    }

    private Task<bool> AuthorizedAsync()
        => _permissionService.AuthorizeAsync(CheckEnginePermissionProvider.ManageCheckEngine.SystemName);

    private static JsonResult BadRequestJson(object body)
        => new(body) { StatusCode = 400 };

    private JsonResult AccessDeniedData()
        => new(new { success = false, errorCode = "admin.access_denied" }) { StatusCode = 403 };
}
