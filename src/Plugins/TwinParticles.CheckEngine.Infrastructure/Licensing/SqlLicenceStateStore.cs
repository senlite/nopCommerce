using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LinqToDB.Data;
using Nop.Data;
using TwinParticles.CheckEngine.Domain.Licensing;

namespace TwinParticles.CheckEngine.Infrastructure.Licensing;

/// <summary>
/// Durable licence heartbeat state shared across web-farm nodes.
/// Activation authority remains external; this only persists the last successful heartbeat.
/// </summary>
public sealed class SqlLicenceStateStore : ILicenceStateStore
{
    private readonly INopDataProvider _dataProvider;

    public SqlLicenceStateStore(INopDataProvider dataProvider)
    {
        _dataProvider = dataProvider;
    }

    public async Task<DateTimeOffset?> GetLastHeartbeatUtcAsync(CancellationToken cancellationToken)
    {
        var rows = await _dataProvider.QueryAsync<LicenceStateRow>(
            "SELECT TOP 1 LastHeartbeatUtc FROM TP_CE_LicenceState ORDER BY Id");

        var value = rows.Select(row => row.LastHeartbeatUtc).FirstOrDefault();
        return value == default ? null : new DateTimeOffset(DateTime.SpecifyKind(value, DateTimeKind.Utc));
    }

    public async Task SetLastHeartbeatUtcAsync(DateTimeOffset heartbeatUtc, CancellationToken cancellationToken)
    {
        var utc = heartbeatUtc.UtcDateTime;
        var updated = await _dataProvider.ExecuteNonQueryAsync(
            @"UPDATE TP_CE_LicenceState
SET LastHeartbeatUtc = @heartbeatUtc, UpdatedUtc = @updatedUtc
WHERE Id = 1",
            new DataParameter("heartbeatUtc", utc),
            new DataParameter("updatedUtc", DateTime.UtcNow));

        if (updated > 0)
            return;

        await _dataProvider.ExecuteNonQueryAsync(
            @"INSERT INTO TP_CE_LicenceState (Id, LastHeartbeatUtc, UpdatedUtc)
VALUES (1, @heartbeatUtc, @updatedUtc)",
            new DataParameter("heartbeatUtc", utc),
            new DataParameter("updatedUtc", DateTime.UtcNow));
    }

    private sealed class LicenceStateRow
    {
        public DateTime LastHeartbeatUtc { get; set; }
    }
}
