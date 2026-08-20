using System;
using System.Threading;
using System.Threading.Tasks;

namespace TwinParticles.CheckEngine.Domain.Tenancy;

public sealed class TenantSetting
{
    public int TenantId { get; set; }

    public string Key { get; set; } = string.Empty;

    public string Value { get; set; } = string.Empty;
}

public interface ITenantSettingsStore
{
    Task<string?> GetAsync(int tenantId, string key, CancellationToken cancellationToken);

    Task UpsertAsync(TenantSetting setting, CancellationToken cancellationToken);
}
