using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Nop.Web.Controllers;
using TwinParticles.CheckEngine.Application.Tenancy;
using TwinParticles.CheckEngine.Domain.Fitment;
using TwinParticles.CheckEngine.Domain.Search;
using TwinParticles.CheckEngine.Domain.Tenancy;

namespace TwinParticles.CheckEngine.Controllers;

/// <summary>
/// Horizon 5 public REST surface (FR-449 / FR-1331). Unauthenticated calls return 401, never catalog data.
/// </summary>
public sealed class PublicApiController : BasePublicController
{
    private readonly PublicApiService _api;

    public PublicApiController(PublicApiService api)
    {
        _api = api;
    }

    [HttpGet]
    [IgnoreAntiforgeryToken]
    public Task<IActionResult> Vehicles(CancellationToken cancellationToken)
        => InvokeAsync(PublicApiScopes.VehiclesRead, auth => _api.ListMakesAsync(auth, cancellationToken), cancellationToken);

    [HttpPost]
    [IgnoreAntiforgeryToken]
    public Task<IActionResult> VinDecode([FromBody] PublicApiVinRequest? model, CancellationToken cancellationToken)
        => InvokeAsync(PublicApiScopes.VinDecode, auth => _api.DecodeVinAsync(auth, model?.Vin ?? string.Empty, cancellationToken), cancellationToken);

    [HttpPost]
    [IgnoreAntiforgeryToken]
    public Task<IActionResult> OemResolve([FromBody] PublicApiOemRequest? model, CancellationToken cancellationToken)
        => InvokeAsync(PublicApiScopes.OemResolve, auth => _api.ResolveOemAsync(auth, model?.Number ?? string.Empty, model?.ManufacturerId, cancellationToken), cancellationToken);

    [HttpPost]
    [IgnoreAntiforgeryToken]
    public Task<IActionResult> FitmentEvaluate([FromBody] PublicApiFitmentRequest? model, CancellationToken cancellationToken)
        => InvokeAsync(PublicApiScopes.FitmentEvaluate, auth => _api.EvaluateFitmentAsync(auth, new FitmentEvaluationContext
        {
            ProductId = model?.ProductId ?? 0,
            VehicleConfigurationId = model?.VehicleConfigurationId ?? 0,
            ProductionYear = model?.ProductionYear,
            SteeringSide = model?.SteeringSide,
            MarketRegion = model?.MarketRegion,
            DriveType = model?.DriveType,
            TransmissionType = model?.TransmissionType
        }, cancellationToken), cancellationToken);

    [HttpPost]
    [IgnoreAntiforgeryToken]
    public Task<IActionResult> Search([FromBody] PublicApiSearchRequest? model, CancellationToken cancellationToken)
        => InvokeAsync(PublicApiScopes.Search, auth => _api.SearchAsync(auth, new SearchQuery
        {
            RawText = model?.Query ?? string.Empty,
            VehicleConfigurationId = model?.VehicleConfigurationId,
            Page = model?.Page > 0 ? model.Page.Value : 1,
            PageSize = model?.PageSize > 0 ? model.PageSize.Value : 24,
            Locale = string.IsNullOrWhiteSpace(model?.Locale) ? "en" : model.Locale
        }, cancellationToken), cancellationToken);

    [HttpGet]
    [IgnoreAntiforgeryToken]
    public Task<IActionResult> Webhooks(CancellationToken cancellationToken)
        => InvokeAsync(PublicApiScopes.WebhooksManage, auth => _api.ListWebhooksAsync(auth, cancellationToken), cancellationToken);

    [HttpPost]
    [IgnoreAntiforgeryToken]
    public Task<IActionResult> RegisterWebhook([FromBody] PublicApiWebhookRequest? model, CancellationToken cancellationToken)
        => InvokeAsync(PublicApiScopes.WebhooksManage, auth => _api.RegisterWebhookAsync(auth, model?.Url, model?.EventTypes, cancellationToken), cancellationToken);

    private async Task<IActionResult> InvokeAsync(
        string scope,
        System.Func<PublicApiAuthentication, Task<object?>> work,
        CancellationToken cancellationToken)
    {
        var auth = await _api.AuthenticateAsync(ReadPresentedKey(), cancellationToken);
        if (!auth.Success)
            return StatusCode(401, new { reasonCode = auth.ReasonCode ?? TenantErrorCodes.PublicApiUnauthenticated });
        if (!_api.TryAcquire(auth, out var retryAfter))
            return StatusCode(429, new { reasonCode = TenantErrorCodes.PublicApiRateLimited, retryAfterSeconds = retryAfter });
        if (!_api.AuthorizeScope(auth, scope))
            return StatusCode(403, new { reasonCode = TenantErrorCodes.PublicApiScopeDenied });

        var quota = await _api.ConsumeQuotaAsync(auth, scope, cancellationToken);
        if (quota == TenantErrorCodes.UsageLimitExceeded)
            return StatusCode(429, new { reasonCode = quota });

        var result = await work(auth);
        if (result is null)
            return BadRequest(new { reasonCode = TenantErrorCodes.WebhookInvalidUrl });
        return Json(result);
    }

    private string? ReadPresentedKey()
    {
        if (Request.Headers.TryGetValue("X-CE-Api-Key", out var header) && !string.IsNullOrWhiteSpace(header))
            return header.ToString();

        var authorization = Request.Headers.Authorization.ToString();
        if (authorization.StartsWith("Bearer ", System.StringComparison.OrdinalIgnoreCase))
            return authorization["Bearer ".Length..].Trim();

        return null;
    }
}

public sealed class PublicApiVinRequest
{
    public string? Vin { get; set; }
}

public sealed class PublicApiOemRequest
{
    public string? Number { get; set; }

    public int? ManufacturerId { get; set; }
}

public sealed class PublicApiFitmentRequest
{
    public int ProductId { get; set; }

    public int VehicleConfigurationId { get; set; }

    public int? ProductionYear { get; set; }

    public string? SteeringSide { get; set; }

    public string? MarketRegion { get; set; }

    public string? DriveType { get; set; }

    public string? TransmissionType { get; set; }
}

public sealed class PublicApiSearchRequest
{
    public string? Query { get; set; }

    public int? VehicleConfigurationId { get; set; }

    public int? Page { get; set; }

    public int? PageSize { get; set; }

    public string? Locale { get; set; }
}

public sealed class PublicApiWebhookRequest
{
    public string? Url { get; set; }

    public string? EventTypes { get; set; }
}
