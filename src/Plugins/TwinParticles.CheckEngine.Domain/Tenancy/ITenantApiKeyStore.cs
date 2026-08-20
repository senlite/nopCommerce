using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace TwinParticles.CheckEngine.Domain.Tenancy;

public sealed class TenantApiKey
{
    public int Id { get; set; }

    public int TenantId { get; set; }

    public string KeyPrefix { get; set; } = string.Empty;

    public string KeyHash { get; set; } = string.Empty;

    public string ScopesCsv { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    public DateTimeOffset CreatedUtc { get; set; }

    public DateTimeOffset? LastUsedUtc { get; set; }
}

public interface ITenantApiKeyStore
{
    Task<int> InsertAsync(TenantApiKey key, CancellationToken cancellationToken);

    Task<TenantApiKey?> GetByHashAsync(string keyHash, CancellationToken cancellationToken);

    Task<IReadOnlyList<TenantApiKey>> ListByTenantAsync(int tenantId, CancellationToken cancellationToken);

    Task SetActiveAsync(int keyId, bool isActive, CancellationToken cancellationToken);

    Task TouchLastUsedAsync(int keyId, DateTimeOffset utc, CancellationToken cancellationToken);
}
