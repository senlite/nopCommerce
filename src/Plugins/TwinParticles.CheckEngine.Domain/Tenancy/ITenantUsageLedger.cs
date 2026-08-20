using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace TwinParticles.CheckEngine.Domain.Tenancy;

/// <summary>FR-1320 usage event captured for metered billing. Invoicing stays behind a port.</summary>
public sealed class TenantUsageEvent
{
    public int TenantId { get; init; }

    public string Metric { get; init; } = string.Empty;

    public decimal Quantity { get; init; } = 1m;

    public DateTimeOffset OccurredUtc { get; init; }
}

public sealed class TenantUsageDaily
{
    public int TenantId { get; init; }

    public string Metric { get; init; } = string.Empty;

    public DateTime DayUtc { get; init; }

    public decimal Quantity { get; init; }
}

public interface ITenantUsageLedger
{
    Task RecordAsync(TenantUsageEvent usageEvent, CancellationToken cancellationToken);

    Task<IReadOnlyList<TenantUsageDaily>> ListDailyAsync(int tenantId, DateTime fromUtc, DateTime toUtc, CancellationToken cancellationToken);
}
