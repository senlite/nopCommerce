using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Nop.Web.Framework;
using Nop.Web.Framework.Controllers;
using Nop.Web.Framework.Mvc.Filters;
using TwinParticles.CheckEngine.Application.Oem;
using TwinParticles.CheckEngine.Models;
using TwinParticles.CheckEngine.Security;

namespace TwinParticles.CheckEngine.Controllers;

[AuthorizeAdmin]
[Area(AreaNames.Admin)]
[AutoValidateAntiforgeryToken]
public sealed class OemAdminController : BasePluginController
{
    private readonly OemAdminService _service;
    private readonly Nop.Services.Security.IPermissionService _permissionService;

    public OemAdminController(OemAdminService service, Nop.Services.Security.IPermissionService permissionService)
    {
        _service = service;
        _permissionService = permissionService;
    }

    private async Task<bool> AuthorizedAsync() => await _permissionService.AuthorizeAsync(CheckEnginePermissionProvider.ManageCheckEngine.SystemName);

    [HttpGet]
    public async Task<IActionResult> Manufacturers(CancellationToken cancellationToken)
    {
        if (!await AuthorizedAsync()) return AccessDeniedView();
        return Json(await _service.GetManufacturersAsync(cancellationToken));
    }

    [HttpPost]
    public async Task<IActionResult> CreateManufacturer([FromBody] OemAdminDtos.ManufacturerUpsertModel model, CancellationToken cancellationToken)
    {
        if (!await AuthorizedAsync()) return AccessDeniedView();
        await _service.CreateManufacturerAsync(model, cancellationToken);
        return Ok();
    }

    [HttpPost]
    public async Task<IActionResult> UpdateManufacturer([FromBody] OemAdminDtos.ManufacturerUpsertModel model, CancellationToken cancellationToken)
    {
        if (!await AuthorizedAsync()) return AccessDeniedView();
        await _service.UpdateManufacturerAsync(model, cancellationToken);
        return Ok();
    }

    [HttpPost]
    public async Task<IActionResult> DeleteManufacturer(int id, CancellationToken cancellationToken)
    {
        if (!await AuthorizedAsync()) return AccessDeniedView();
        await _service.DeleteManufacturerAsync(id, cancellationToken);
        return Ok();
    }

    [HttpGet]
    public async Task<IActionResult> OemNumbers(CancellationToken cancellationToken)
    {
        if (!await AuthorizedAsync()) return AccessDeniedView();
        return Json(await _service.GetOemNumbersAsync(cancellationToken));
    }

    [HttpPost]
    public async Task<IActionResult> CreateOemNumber([FromBody] OemAdminDtos.OemNumberUpsertModel model, CancellationToken cancellationToken)
    {
        if (!await AuthorizedAsync()) return AccessDeniedView();
        await _service.CreateOemNumberAsync(model, cancellationToken);
        return Ok();
    }

    [HttpPost]
    public async Task<IActionResult> UpdateOemNumber([FromBody] OemAdminDtos.OemNumberUpsertModel model, CancellationToken cancellationToken)
    {
        if (!await AuthorizedAsync()) return AccessDeniedView();
        await _service.UpdateOemNumberAsync(model, cancellationToken);
        return Ok();
    }

    [HttpPost]
    public async Task<IActionResult> DeleteOemNumber(int id, CancellationToken cancellationToken)
    {
        if (!await AuthorizedAsync()) return AccessDeniedView();
        await _service.DeleteOemNumberAsync(id, cancellationToken);
        return Ok();
    }

    [HttpGet]
    public async Task<IActionResult> Relations(CancellationToken cancellationToken)
    {
        if (!await AuthorizedAsync()) return AccessDeniedView();
        return Json(await _service.GetRelationsAsync(cancellationToken));
    }

    [HttpPost]
    public async Task<IActionResult> CreateRelation([FromBody] OemAdminDtos.OemRelationUpsertModel model, CancellationToken cancellationToken)
    {
        if (!await AuthorizedAsync()) return AccessDeniedView();
        await _service.CreateRelationAsync(model, cancellationToken);
        return Ok();
    }

    [HttpPost]
    public async Task<IActionResult> UpdateRelation([FromBody] OemAdminDtos.OemRelationUpsertModel model, CancellationToken cancellationToken)
    {
        if (!await AuthorizedAsync()) return AccessDeniedView();
        await _service.UpdateRelationAsync(model, cancellationToken);
        return Ok();
    }

    [HttpPost]
    public async Task<IActionResult> DeleteRelation(int id, CancellationToken cancellationToken)
    {
        if (!await AuthorizedAsync()) return AccessDeniedView();
        await _service.DeleteRelationAsync(id, cancellationToken);
        return Ok();
    }
}
