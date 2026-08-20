using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TwinParticles.CheckEngine.Application.Fitment;
using TwinParticles.CheckEngine.Application.Oem;
using TwinParticles.CheckEngine.Application.Search;
using TwinParticles.CheckEngine.Application.Vehicle.Admin;
using TwinParticles.CheckEngine.Application.Vehicle.Vin;
using TwinParticles.CheckEngine.Domain.Fitment;
using TwinParticles.CheckEngine.Domain.Search;
using TwinParticles.CheckEngine.Domain.Tenancy;

namespace TwinParticles.CheckEngine.Application.Tenancy;

public sealed class PublicApiService
{
    private readonly PublicApiKeyService _keys;
    private readonly IPublicApiRateLimiter _rateLimiter;
    private readonly ITenantUsageLedger _usage;
    private readonly TenantUsageLimitService _limits;
    private readonly TenantWebhookService _webhooks;
    private readonly FitmentEvaluationService _fitment;
    private readonly VinDecodeApplicationService _vin;
    private readonly OemResolveService _oem;
    private readonly UnifiedSearchService _search;
    private readonly VehicleAdminService _vehicles;

    public PublicApiService(
        PublicApiKeyService keys,
        IPublicApiRateLimiter rateLimiter,
        ITenantUsageLedger usage,
        TenantUsageLimitService limits,
        TenantWebhookService webhooks,
        FitmentEvaluationService fitment,
        VinDecodeApplicationService vin,
        OemResolveService oem,
        UnifiedSearchService search,
        VehicleAdminService vehicles)
    {
        _keys = keys;
        _rateLimiter = rateLimiter;
        _usage = usage;
        _limits = limits;
        _webhooks = webhooks;
        _fitment = fitment;
        _vin = vin;
        _oem = oem;
        _search = search;
        _vehicles = vehicles;
    }

    public Task<PublicApiAuthentication> AuthenticateAsync(string? presentedKey, CancellationToken cancellationToken)
        => _keys.AuthenticateAsync(presentedKey, cancellationToken);

    public bool TryAcquire(PublicApiAuthentication auth, out int retryAfterSeconds)
    {
        retryAfterSeconds = 0;
        if (!auth.Success || auth.ApiKey is null)
            return false;
        return _rateLimiter.TryAcquire("public-api:" + auth.ApiKey.Id, out retryAfterSeconds);
    }

    public bool AuthorizeScope(PublicApiAuthentication auth, string scope)
        => auth.Success && auth.ApiKey is not null && PublicApiKeyService.HasScope(auth.ApiKey, scope);

    public async Task<string?> ConsumeQuotaAsync(PublicApiAuthentication auth, string metric, CancellationToken cancellationToken)
    {
        if (auth.Tenant is null)
            return TenantErrorCodes.PublicApiUnauthenticated;
        if (await _limits.IsLimitedAsync(auth.Tenant.Id, metric, cancellationToken))
            return TenantErrorCodes.UsageLimitExceeded;

        await _usage.RecordAsync(new TenantUsageEvent
        {
            TenantId = auth.Tenant.Id,
            Metric = metric,
            Quantity = 1m,
            OccurredUtc = DateTimeOffset.UtcNow
        }, cancellationToken);
        return null;
    }

    public async Task<object?> ListMakesAsync(PublicApiAuthentication auth, CancellationToken cancellationToken)
    {
        var makes = await _vehicles.GetMakesAsync(cancellationToken);
        var payload = new
        {
            apiVersion = "v1",
            items = makes.Select(make => new { id = make.Id, name = make.Name }).ToList()
        };
        await _webhooks.DispatchAsync(auth.Tenant, TenantWebhookEvents.VehiclesListed, payload, cancellationToken);
        return payload;
    }

    public async Task<object?> DecodeVinAsync(PublicApiAuthentication auth, string vin, CancellationToken cancellationToken)
    {
        var result = await _vin.DecodeAsync(vin, cancellationToken);
        var payload = new
        {
            apiVersion = "v1",
            result.Outcome,
            result.NormalizedVin,
            result.CheckDigitValid,
            result.Wmi,
            result.ReasonCode,
            candidates = result.Candidates.Select(c => new
            {
                vehicleConfigurationId = c.VehicleConfigurationId,
                confidence = c.Confidence.Value,
                modelYear = c.ModelYear
            })
        };
        await _webhooks.DispatchAsync(auth.Tenant, TenantWebhookEvents.VinDecoded, payload, cancellationToken);
        return payload;
    }

    public async Task<object?> ResolveOemAsync(PublicApiAuthentication auth, string number, int? manufacturerId, CancellationToken cancellationToken)
    {
        var result = await _oem.ResolveAsync(new OemResolveQuery { Number = number, ManufacturerId = manufacturerId }, cancellationToken);
        var payload = new
        {
            apiVersion = "v1",
            success = result.Success,
            errorCode = result.ErrorCode,
            oemNumberId = result.OemNumberId,
            manufacturerId = result.ManufacturerId,
            displayNumber = result.DisplayNumber,
            normalizedNumber = result.NormalizedNumber,
            isObsolete = result.IsObsolete,
            currentOemNumberId = result.CurrentOemNumberId,
            productIds = result.ProductIds
        };
        await _webhooks.DispatchAsync(auth.Tenant, TenantWebhookEvents.OemResolved, payload, cancellationToken);
        return payload;
    }

    public async Task<object?> EvaluateFitmentAsync(
        PublicApiAuthentication auth,
        FitmentEvaluationContext context,
        CancellationToken cancellationToken)
    {
        var result = await _fitment.EvaluateAsync(context, cancellationToken);
        var payload = new
        {
            apiVersion = "v1",
            outcome = result.Outcome.ToString(),
            confidence = result.EffectiveConfidence,
            reasonCode = result.ReasonCode
        };
        await _webhooks.DispatchAsync(auth.Tenant, TenantWebhookEvents.FitmentEvaluated, payload, cancellationToken);
        return payload;
    }

    public async Task<object?> SearchAsync(PublicApiAuthentication auth, SearchQuery query, CancellationToken cancellationToken)
    {
        var result = await _search.SearchAsync(query, cancellationToken);
        var payload = new
        {
            apiVersion = "v1",
            modeUsed = result.ModeUsed.ToString(),
            total = result.Total,
            isDegraded = result.IsDegraded,
            needsDisambiguation = result.NeedsDisambiguation,
            hits = result.Hits.Select(h => new
            {
                h.ProductId,
                h.Name,
                h.Brand,
                h.Price,
                h.Score,
                h.FitsActiveContext
            })
        };
        await _webhooks.DispatchAsync(auth.Tenant, TenantWebhookEvents.SearchCompleted, payload, cancellationToken);
        return payload;
    }

    public async Task<object?> RegisterWebhookAsync(
        PublicApiAuthentication auth,
        string? targetUrl,
        string? eventTypesCsv,
        CancellationToken cancellationToken)
    {
        if (auth.Tenant is null)
            return null;
        var issued = await _webhooks.RegisterAsync(
            TenantActor.ForTenant(auth.Tenant.Id),
            auth.Tenant.Id,
            targetUrl,
            eventTypesCsv,
            cancellationToken);
        if (issued is null)
            return null;
        return new
        {
            apiVersion = "v1",
            id = issued.Record.Id,
            url = issued.Record.TargetUrl,
            eventTypes = issued.Record.EventTypesCsv,
            plaintextSecret = issued.PlaintextSecret
        };
    }

    public async Task<object?> ListWebhooksAsync(PublicApiAuthentication auth, CancellationToken cancellationToken)
    {
        if (auth.Tenant is null)
            return new { apiVersion = "v1", items = Array.Empty<object>() };

        var rows = await _webhooks.ListAsync(TenantActor.ForTenant(auth.Tenant.Id), auth.Tenant.Id, cancellationToken);
        return new
        {
            apiVersion = "v1",
            items = rows.Select(row => new
            {
                row.Id,
                url = row.TargetUrl,
                eventTypes = row.EventTypesCsv,
                row.IsActive,
                createdUtc = row.CreatedUtc
            })
        };
    }
}
