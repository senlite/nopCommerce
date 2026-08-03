using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Nop.Web.Framework;
using Nop.Web.Framework.Controllers;
using Nop.Web.Framework.Mvc.Filters;
using TwinParticles.CheckEngine.Application.Search;
using TwinParticles.CheckEngine.Security;

namespace TwinParticles.CheckEngine.Controllers;

[AuthorizeAdmin]
[Area(AreaNames.Admin)]
[AutoValidateAntiforgeryToken]
public sealed class SearchAdminController : BasePluginController
{
    private readonly Nop.Services.Security.IPermissionService _permissionService;
    private readonly SearchIndexAdminService _service;

    public SearchAdminController(SearchIndexAdminService service, Nop.Services.Security.IPermissionService permissionService)
    {
        _service = service;
        _permissionService = permissionService;
    }

    private async Task<bool> AuthorizedAsync() => await _permissionService.AuthorizeAsync(CheckEnginePermissionProvider.ManageCheckEngine.SystemName);

    [HttpPost]
    public async Task<IActionResult> Rebuild(CancellationToken cancellationToken)
    {
        if (!await AuthorizedAsync()) return AccessDeniedView();

        await _service.RebuildAsync(cancellationToken);
        return Ok();
    }
}
