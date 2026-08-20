using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Nop.Web.Framework;
using Nop.Web.Framework.Controllers;
using Nop.Web.Framework.Mvc.Filters;
using TwinParticles.CheckEngine.Application.Tenancy;
using TwinParticles.CheckEngine.Domain.Tenancy;
using TwinParticles.CheckEngine.Infrastructure;
using TwinParticles.CheckEngine.Security;

namespace TwinParticles.CheckEngine.Controllers;

[AuthorizeAdmin]
[Area(AreaNames.ADMIN)]
[AutoValidateAntiforgeryToken]
public sealed class TenantAdminController : BasePluginController
{
    private readonly TenantRegistryService _registry;
    private readonly PublicApiKeyService _keys;
    private readonly ITenantUsageLedger _usage;
    private readonly ITenantApiKeyStore _keyStore;
    private readonly Nop.Services.Security.IPermissionService _permissionService;

    public TenantAdminController(
        TenantRegistryService registry,
        PublicApiKeyService keys,
        ITenantUsageLedger usage,
        ITenantApiKeyStore keyStore,
        Nop.Services.Security.IPermissionService permissionService)
    {
        _registry = registry;
        _keys = keys;
        _usage = usage;
        _keyStore = keyStore;
        _permissionService = permissionService;
    }

    private async Task<bool> AuthorizedAsync()
        => await _permissionService.AuthorizeAsync(CheckEnginePermissionProvider.ManageCheckEngine.SystemName);

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        if (!await AuthorizedAsync())
            return AccessDeniedView();

        await _registry.EnsureSelfHostedAsync(cancellationToken);
        return View("~/Plugins/TwinParticles.CheckEngine/Views/Admin/TenantAdmin.cshtml");
    }

    [HttpGet]
    public async Task<IActionResult> Tenants(CancellationToken cancellationToken)
    {
        if (!await AuthorizedAsync())
            return AccessDeniedView();
        if (!Request.WantsJsonResponse())
            return CheckEnginePaths.RedirectAdmin("TenantAdmin", "Index");

        await _registry.EnsureSelfHostedAsync(cancellationToken);
        var tenants = await _registry.ListAsync(cancellationToken);
        return Json(new
        {
            items = tenants.Select(tenant => new
            {
                tenant.Id,
                tenant.Slug,
                tenant.DisplayName,
                status = tenant.Status.ToString(),
                tenant.HostnamesCsv,
                tenant.IsSelfHosted,
                tenant.IsolationMode,
                tenant.ConnectionName
            })
        });
    }

    [HttpPost]
    public async Task<IActionResult> Provision([FromBody] TenantProvisionModel? model, CancellationToken cancellationToken)
    {
        if (!await AuthorizedAsync())
            return AccessDeniedView();

        var result = await _registry.ProvisionAsync(
            TenantActor.ControlPlaneOperator,
            model?.Slug ?? string.Empty,
            model?.DisplayName ?? string.Empty,
            model?.HostnamesCsv,
            model?.ConnectionName,
            cancellationToken);
        if (!result.Success)
            return BadRequest(new { reasonCode = result.ReasonCode });
        return Json(result.Tenant);
    }

    [HttpPost]
    public async Task<IActionResult> Suspend([FromBody] TenantStatusModel? model, CancellationToken cancellationToken)
    {
        if (!await AuthorizedAsync())
            return AccessDeniedView();

        var decision = await _registry.SetStatusAsync(
            TenantActor.ControlPlaneOperator,
            model?.TenantId ?? 0,
            TenantStatus.Suspended,
            cancellationToken);
        if (!decision.Allowed)
            return BadRequest(new { reasonCode = decision.ReasonCode });
        return Json(new { ok = true });
    }

    [HttpPost]
    public async Task<IActionResult> IssueApiKey([FromBody] TenantStatusModel? model, CancellationToken cancellationToken)
    {
        if (!await AuthorizedAsync())
            return AccessDeniedView();

        var issued = await _keys.IssueAsync(TenantActor.ControlPlaneOperator, model?.TenantId ?? 0, null, cancellationToken);
        if (issued is null)
            return BadRequest(new { reasonCode = TenantErrorCodes.TenantNotFound });
        return Json(new
        {
            id = issued.Record.Id,
            tenantId = issued.Record.TenantId,
            keyPrefix = issued.Record.KeyPrefix,
            plaintext = issued.Plaintext,
            scopes = issued.Record.ScopesCsv
        });
    }

    [HttpGet]
    public async Task<IActionResult> Usage(int tenantId, CancellationToken cancellationToken)
    {
        if (!await AuthorizedAsync())
            return AccessDeniedView();
        if (!Request.WantsJsonResponse())
            return CheckEnginePaths.RedirectAdmin("TenantAdmin", "Index");

        var from = DateTime.UtcNow.Date.AddDays(-30);
        var rows = await _usage.ListDailyAsync(tenantId, from, DateTime.UtcNow.Date, cancellationToken);
        var keys = await _keyStore.ListByTenantAsync(tenantId, cancellationToken);
        return Json(new
        {
            usage = rows,
            apiKeys = keys.Select(key => new
            {
                key.Id,
                key.KeyPrefix,
                key.IsActive,
                key.CreatedUtc,
                key.LastUsedUtc,
                scopes = key.ScopesCsv
            })
        });
    }
}

public sealed class TenantProvisionModel
{
    public string? Slug { get; set; }

    public string? DisplayName { get; set; }

    public string? HostnamesCsv { get; set; }

    public string? ConnectionName { get; set; }
}

public sealed class TenantStatusModel
{
    public int TenantId { get; set; }
}
