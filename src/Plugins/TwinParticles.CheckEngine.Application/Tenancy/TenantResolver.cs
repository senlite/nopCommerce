using System;
using System.Threading;
using System.Threading.Tasks;
using TwinParticles.CheckEngine.Domain.Tenancy;

namespace TwinParticles.CheckEngine.Application.Tenancy;

public sealed class TenantResolutionRequest
{
    public string? Hostname { get; init; }

    public string? Slug { get; init; }

    public int? TenantId { get; init; }
}

public sealed class TenantResolver
{
    private readonly ITenantRegistry _registry;
    private readonly TenantRegistryService _registryService;
    private readonly ITenantAccessor _accessor;

    public TenantResolver(ITenantRegistry registry, TenantRegistryService registryService, ITenantAccessor accessor)
    {
        _registry = registry;
        _registryService = registryService;
        _accessor = accessor;
    }

    public async Task<Tenant?> ResolveAsync(TenantResolutionRequest request, CancellationToken cancellationToken)
    {
        Tenant? tenant = null;
        if (request.TenantId is int tenantId && tenantId > 0)
            tenant = await _registry.GetByIdAsync(tenantId, cancellationToken);

        if (tenant is null && !string.IsNullOrWhiteSpace(request.Slug))
            tenant = await _registry.GetBySlugAsync(TenantRegistryService.NormalizeSlug(request.Slug), cancellationToken);

        if (tenant is null && !string.IsNullOrWhiteSpace(request.Hostname))
            tenant = await _registry.GetByHostnameAsync(request.Hostname.Trim().ToLowerInvariant(), cancellationToken);

        tenant ??= await _registryService.EnsureSelfHostedAsync(cancellationToken);
        if (tenant.Status is TenantStatus.Suspended or TenantStatus.Closed)
            return tenant;

        _registryService.Bind(tenant);
        return tenant;
    }

    public TenantContext Current => _accessor.Current;
}
