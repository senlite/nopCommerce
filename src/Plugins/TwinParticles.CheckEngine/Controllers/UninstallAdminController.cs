using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Nop.Core;
using Nop.Services.Configuration;
using Nop.Web.Framework;
using Nop.Web.Framework.Controllers;
using Nop.Web.Framework.Mvc.Filters;
using TwinParticles.CheckEngine.Application.Observability;
using TwinParticles.CheckEngine.Configuration;
using TwinParticles.CheckEngine.Infrastructure;
using TwinParticles.CheckEngine.Security;

namespace TwinParticles.CheckEngine.Controllers;

[AuthorizeAdmin]
[Area(AreaNames.ADMIN)]
[AutoValidateAntiforgeryToken]
public sealed class UninstallAdminController : BasePluginController
{
    private static readonly TimeSpan PreparationLifetime = TimeSpan.FromHours(24);

    private readonly CheckEngineUninstallExportService _exportService;
    private readonly Nop.Services.Security.IPermissionService _permissionService;
    private readonly ISettingService _settingService;
    private readonly IWorkContext _workContext;

    public UninstallAdminController(
        CheckEngineUninstallExportService exportService,
        Nop.Services.Security.IPermissionService permissionService,
        ISettingService settingService,
        IWorkContext workContext)
    {
        _exportService = exportService;
        _permissionService = permissionService;
        _settingService = settingService;
        _workContext = workContext;
    }

    [HttpGet]
    public async Task<IActionResult> Status()
    {
        if (!await AuthorizedAsync()) return AccessDeniedView();
        if (!Request.WantsJsonResponse())
            return CheckEnginePaths.RedirectAdmin("CheckEngine", "Dashboard");

        var settings = await _settingService.LoadSettingAsync<CheckEnginePluginSettings>();
        var preparedUtc = settings.UninstallExportPreparedUtc;
        return Json(new
        {
            warning = "Uninstall permanently deletes Check Engine vehicle, OEM, fitment, garage, analytics, audit and integration data.",
            exportRequired = true,
            preparedUtc,
            prepared = preparedUtc.HasValue && DateTime.UtcNow - preparedUtc.Value <= PreparationLifetime,
            expiresUtc = preparedUtc?.Add(PreparationLifetime),
            exportUrl = "/Admin/CheckEngine/UninstallAdmin/Export"
        });
    }

    [HttpGet]
    public async Task<IActionResult> Export(CancellationToken cancellationToken)
    {
        if (!await AuthorizedAsync()) return AccessDeniedView();
        var customer = await _workContext.GetCurrentCustomerAsync();
        var export = await _exportService.ExportAsync($"customer:{customer.Id}", cancellationToken);
        var bytes = JsonSerializer.SerializeToUtf8Bytes(export, new JsonSerializerOptions
        {
            WriteIndented = true
        });

        var settings = await _settingService.LoadSettingAsync<CheckEnginePluginSettings>();
        settings.UninstallExportPreparedUtc = DateTime.UtcNow;
        await _settingService.SaveSettingAsync(settings);

        var filename = $"check-engine-uninstall-export-{DateTime.UtcNow:yyyyMMdd-HHmmss}.json";
        return File(bytes, "application/json", filename);
    }

    private Task<bool> AuthorizedAsync()
        => _permissionService.AuthorizeAsync(CheckEnginePermissionProvider.ManageCheckEngine.SystemName);
}
