using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Nop.Web.Framework;
using Nop.Web.Framework.Controllers;
using Nop.Web.Framework.Mvc.Filters;
using TwinParticles.CheckEngine.Application.Garage;
using TwinParticles.CheckEngine.Security;

namespace TwinParticles.CheckEngine.Controllers;

[AuthorizeAdmin]
[Area(AreaNames.ADMIN)]
[AutoValidateAntiforgeryToken]
public sealed class GarageAdminController : BasePluginController
{
    private readonly GarageService _garageService;
    private readonly Nop.Services.Security.IPermissionService _permissionService;

    public GarageAdminController(GarageService garageService, Nop.Services.Security.IPermissionService permissionService)
    {
        _garageService = garageService;
        _permissionService = permissionService;
    }

    private async Task<bool> AuthorizedAsync() => await _permissionService.AuthorizeAsync(CheckEnginePermissionProvider.ManageCheckEngine.SystemName);

    [HttpGet]
    public async Task<IActionResult> CustomerGarage(int customerId, CancellationToken cancellationToken)
    {
        if (!await AuthorizedAsync()) return AccessDeniedView();

        var garage = await _garageService.AdminViewAsync(customerId, cancellationToken);
        if (garage is null)
            return NotFound();

        return Json(garage);
    }
}
