using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Nop.Web.Framework;
using Nop.Web.Framework.Controllers;
using Nop.Web.Framework.Mvc.Filters;
using TwinParticles.CheckEngine.Application.Observability;
using TwinParticles.CheckEngine.Domain.Licensing;
using TwinParticles.CheckEngine.Domain.Search;
using TwinParticles.CheckEngine.Security;

namespace TwinParticles.CheckEngine.Controllers;

[AuthorizeAdmin]
[Area(AreaNames.ADMIN)]
[AutoValidateAntiforgeryToken]
public sealed class DiagnosticsAdminController : BasePluginController
{
    private readonly CheckEngineHealthService _healthService;
    private readonly ILicenceService _licenceService;
    private readonly ISearchIndexHealthService _searchIndexHealthService;
    private readonly Nop.Services.Security.IPermissionService _permissionService;

    public DiagnosticsAdminController(
        CheckEngineHealthService healthService,
        ILicenceService licenceService,
        ISearchIndexHealthService searchIndexHealthService,
        Nop.Services.Security.IPermissionService permissionService)
    {
        _healthService = healthService;
        _licenceService = licenceService;
        _searchIndexHealthService = searchIndexHealthService;
        _permissionService = permissionService;
    }

    private async Task<bool> AuthorizedAsync()
        => await _permissionService.AuthorizeAsync(CheckEnginePermissionProvider.ManageCheckEngine.SystemName);

    [HttpGet]
    public async Task<IActionResult> Package(CancellationToken cancellationToken)
    {
        if (!await AuthorizedAsync())
            return AccessDeniedView();

        var health = await _healthService.ProbeAsync(cancellationToken);
        var licence = await _licenceService.GetStatusAsync(cancellationToken);
        var searchHealthy = false;
        try
        {
            searchHealthy = await _searchIndexHealthService.IsHealthyAsync(cancellationToken);
        }
        catch
        {
            // redacted package must remain resilient
        }

        // Intentionally omit secrets, connection strings, licence keys, and payloads.
        return Json(new
        {
            generatedUtc = DateTime.UtcNow,
            plugin = "TwinParticles.CheckEngine",
            health = new
            {
                health.Status,
                health.Database,
                health.SearchIndex,
                health.Erp,
                health.Licence
            },
            licence = new
            {
                licence.IsActive,
                licence.State,
                licence.LastHeartbeatUtc,
                licence.ReasonCode
            },
            searchIndexHealthy = searchHealthy,
            redacted = true
        });
    }
}
