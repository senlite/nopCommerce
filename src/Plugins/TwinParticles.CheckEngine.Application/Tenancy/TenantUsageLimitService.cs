using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using TwinParticles.CheckEngine.Domain.Tenancy;

namespace TwinParticles.CheckEngine.Application.Tenancy;

/// <summary>
/// FR-1322: exceeding a metered plan limit throttles new billable calls and never deletes tenant data.
/// Limits are optional per-tenant settings keyed <c>usage.limit.{metric}</c>.
/// </summary>
public sealed class TenantUsageLimitService
{
    public const string SettingKeyPrefix = "usage.limit.";

    private readonly ITenantSettingsStore _settings;
    private readonly ITenantUsageLedger _ledger;

    public TenantUsageLimitService(ITenantSettingsStore settings, ITenantUsageLedger ledger)
    {
        _settings = settings;
        _ledger = ledger;
    }

    public async Task<bool> IsLimitedAsync(int tenantId, string metric, CancellationToken cancellationToken)
    {
        var raw = await _settings.GetAsync(tenantId, SettingKeyPrefix + metric, cancellationToken);
        if (string.IsNullOrWhiteSpace(raw) ||
            !decimal.TryParse(raw, NumberStyles.Number, CultureInfo.InvariantCulture, out var limit) ||
            limit <= 0m)
        {
            return false;
        }

        var today = System.DateTime.UtcNow.Date;
        var rows = await _ledger.ListDailyAsync(tenantId, today, today, cancellationToken);
        decimal used = 0m;
        foreach (var row in rows)
        {
            if (string.Equals(row.Metric, metric, System.StringComparison.OrdinalIgnoreCase))
                used += row.Quantity;
        }

        return used >= limit;
    }
}
