using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using TwinParticles.CheckEngine.Domain.Tenancy;

namespace TwinParticles.CheckEngine.Application.Tenancy;

public sealed class TenantRegistryService
{
    private static readonly Regex SlugPattern = new("^[a-z0-9][a-z0-9-]{1,62}$", RegexOptions.Compiled);

    private readonly ITenantRegistry _registry;
    private readonly ITenantAccessor _accessor;

    public TenantRegistryService(ITenantRegistry registry, ITenantAccessor accessor)
    {
        _registry = registry;
        _accessor = accessor;
    }

    public async Task<Tenant> EnsureSelfHostedAsync(CancellationToken cancellationToken)
    {
        var existing = await _registry.GetBySlugAsync(SelfHostedTenant.Slug, cancellationToken);
        if (existing is not null)
            return existing;

        var now = DateTimeOffset.UtcNow;
        var tenant = new Tenant
        {
            Slug = SelfHostedTenant.Slug,
            DisplayName = SelfHostedTenant.DisplayName,
            Status = TenantStatus.Active,
            IsolationMode = TenantIsolationMode.DatabasePerTenant,
            ConnectionName = string.Empty,
            IsSelfHosted = true,
            CreatedUtc = now,
            UpdatedUtc = now
        };
        tenant.Id = await _registry.InsertAsync(tenant, cancellationToken);
        return tenant;
    }

    public async Task<TenantProvisionResult> ProvisionAsync(
        TenantActor actor,
        string slug,
        string displayName,
        string? hostnamesCsv,
        string? connectionName,
        CancellationToken cancellationToken)
    {
        if (!actor.CanBypassIsolation)
            return TenantProvisionResult.Fail(TenantErrorCodes.IsolationDenied);

        var normalized = NormalizeSlug(slug);
        if (!SlugPattern.IsMatch(normalized))
            return TenantProvisionResult.Fail(TenantErrorCodes.InvalidSlug);

        if (await _registry.GetBySlugAsync(normalized, cancellationToken) is not null)
            return TenantProvisionResult.Fail(TenantErrorCodes.DuplicateSlug);

        var now = DateTimeOffset.UtcNow;
        var tenant = new Tenant
        {
            Slug = normalized,
            DisplayName = string.IsNullOrWhiteSpace(displayName) ? normalized : displayName.Trim(),
            Status = TenantStatus.Active,
            IsolationMode = TenantIsolationMode.DatabasePerTenant,
            ConnectionName = connectionName?.Trim() ?? string.Empty,
            HostnamesCsv = hostnamesCsv?.Trim() ?? string.Empty,
            IsSelfHosted = false,
            CreatedUtc = now,
            UpdatedUtc = now
        };
        tenant.Id = await _registry.InsertAsync(tenant, cancellationToken);
        return TenantProvisionResult.Ok(tenant);
    }

    public async Task<TenantIsolationDecision> SetStatusAsync(
        TenantActor actor,
        int tenantId,
        TenantStatus status,
        CancellationToken cancellationToken)
    {
        if (!actor.CanBypassIsolation)
            return TenantIsolationDecision.Deny(TenantErrorCodes.IsolationDenied);

        var tenant = await _registry.GetByIdAsync(tenantId, cancellationToken);
        if (tenant is null)
            return TenantIsolationDecision.Deny(TenantErrorCodes.TenantNotFound);

        tenant.Status = status;
        tenant.UpdatedUtc = DateTimeOffset.UtcNow;
        await _registry.UpdateAsync(tenant, cancellationToken);
        return TenantIsolationDecision.Allow();
    }

    public Task<IReadOnlyList<Tenant>> ListAsync(CancellationToken cancellationToken)
        => _registry.ListAsync(cancellationToken);

    public void Bind(Tenant tenant)
    {
        _accessor.SetCurrent(new TenantContext
        {
            TenantId = tenant.Id,
            Slug = tenant.Slug,
            IsolationMode = tenant.IsolationMode,
            ConnectionName = tenant.ConnectionName,
            IsSelfHosted = tenant.IsSelfHosted,
            IsControlPlane = false
        });
    }

    public static string NormalizeSlug(string slug)
        => (slug ?? string.Empty).Trim().ToLowerInvariant();
}

public sealed class TenantProvisionResult
{
    public bool Success { get; init; }

    public Tenant? Tenant { get; init; }

    public string? ReasonCode { get; init; }

    public static TenantProvisionResult Ok(Tenant tenant) => new() { Success = true, Tenant = tenant };

    public static TenantProvisionResult Fail(string reasonCode) => new() { Success = false, ReasonCode = reasonCode };
}
