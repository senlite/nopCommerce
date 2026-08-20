using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LinqToDB.Data;
using Nop.Data;
using TwinParticles.CheckEngine.Domain.Tenancy;

namespace TwinParticles.CheckEngine.Infrastructure.Tenancy;

public sealed class SqlTenantUsageLedger : ITenantUsageLedger
{
    private readonly INopDataProvider _dataProvider;

    public SqlTenantUsageLedger(INopDataProvider dataProvider)
    {
        _dataProvider = dataProvider;
    }

    public async Task RecordAsync(TenantUsageEvent usageEvent, CancellationToken cancellationToken)
    {
        var day = usageEvent.OccurredUtc.UtcDateTime.Date;
        var updated = await _dataProvider.ExecuteNonQueryAsync(@"
UPDATE TP_CE_TenantUsageDaily
SET Quantity = Quantity + @quantity
WHERE TenantId = @tenantId AND Metric = @metric AND DayUtc = @dayUtc",
            new DataParameter("tenantId", usageEvent.TenantId),
            new DataParameter("metric", usageEvent.Metric),
            new DataParameter("dayUtc", day),
            new DataParameter("quantity", usageEvent.Quantity));
        if (updated > 0)
            return;

        await _dataProvider.ExecuteNonQueryAsync(@"
INSERT INTO TP_CE_TenantUsageDaily (TenantId, Metric, DayUtc, Quantity)
VALUES (@tenantId, @metric, @dayUtc, @quantity)",
            new DataParameter("tenantId", usageEvent.TenantId),
            new DataParameter("metric", usageEvent.Metric),
            new DataParameter("dayUtc", day),
            new DataParameter("quantity", usageEvent.Quantity));
    }

    public async Task<IReadOnlyList<TenantUsageDaily>> ListDailyAsync(int tenantId, DateTime fromUtc, DateTime toUtc, CancellationToken cancellationToken)
    {
        var rows = await _dataProvider.QueryAsync<UsageRow>(@"
SELECT TenantId, Metric, DayUtc, Quantity
FROM TP_CE_TenantUsageDaily
WHERE TenantId = @tenantId AND DayUtc >= @fromUtc AND DayUtc <= @toUtc
ORDER BY DayUtc, Metric",
            new DataParameter("tenantId", tenantId),
            new DataParameter("fromUtc", fromUtc.Date),
            new DataParameter("toUtc", toUtc.Date));
        return rows.Select(row => new TenantUsageDaily
        {
            TenantId = row.TenantId,
            Metric = row.Metric,
            DayUtc = DateTime.SpecifyKind(row.DayUtc, DateTimeKind.Utc),
            Quantity = row.Quantity
        }).ToList();
    }

    private sealed class UsageRow
    {
        public int TenantId { get; set; }
        public string Metric { get; set; } = string.Empty;
        public DateTime DayUtc { get; set; }
        public decimal Quantity { get; set; }
    }
}
