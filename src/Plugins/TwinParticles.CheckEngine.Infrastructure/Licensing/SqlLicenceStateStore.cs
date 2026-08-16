using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LinqToDB.Data;
using Nop.Data;
using Nop.Services.Security;
using TwinParticles.CheckEngine.Domain.Licensing;

namespace TwinParticles.CheckEngine.Infrastructure.Licensing;

/// <summary>
/// Durable licence heartbeat state shared across web-farm nodes.
/// The stored key is re-validated on heartbeat so the scheduled task cannot extend grace without a
/// still-valid activation.
/// </summary>
public sealed class SqlLicenceStateStore : ILicenceStateStore
{
    private const string KeyPrefix = "enc:v1:";

    private readonly INopDataProvider _dataProvider;
    private readonly IEncryptionService _encryptionService;

    public SqlLicenceStateStore(INopDataProvider dataProvider, IEncryptionService encryptionService)
    {
        _dataProvider = dataProvider;
        _encryptionService = encryptionService;
    }

    public async Task<DateTimeOffset?> GetLastHeartbeatUtcAsync(CancellationToken cancellationToken)
    {
        var rows = await _dataProvider.QueryAsync<LicenceStateRow>(
            "SELECT LastHeartbeatUtc FROM TP_CE_LicenceState ORDER BY Id");

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

    public async Task<string?> GetActivationKeyAsync(CancellationToken cancellationToken)
    {
        var rows = await _dataProvider.QueryAsync<ActivationKeyRow>(
            "SELECT ActivationKeyProtected FROM TP_CE_LicenceState ORDER BY Id");

        var stored = rows.Select(row => row.ActivationKeyProtected).FirstOrDefault();
        return Unprotect(stored);
    }

    public async Task SetActivationKeyAsync(string licenceKey, CancellationToken cancellationToken)
    {
        var protectedKey = Protect(licenceKey);
        var updated = await _dataProvider.ExecuteNonQueryAsync(
            @"UPDATE TP_CE_LicenceState
SET ActivationKeyProtected = @activationKey, UpdatedUtc = @updatedUtc
WHERE Id = 1",
            new DataParameter("activationKey", protectedKey),
            new DataParameter("updatedUtc", DateTime.UtcNow));

        if (updated > 0)
            return;

        await _dataProvider.ExecuteNonQueryAsync(
            @"INSERT INTO TP_CE_LicenceState (Id, LastHeartbeatUtc, ActivationKeyProtected, UpdatedUtc)
VALUES (1, NULL, @activationKey, @updatedUtc)",
            new DataParameter("activationKey", protectedKey),
            new DataParameter("updatedUtc", DateTime.UtcNow));
    }

    private string Protect(string licenceKey)
    {
        if (licenceKey.StartsWith(KeyPrefix, StringComparison.Ordinal))
            return licenceKey;

        return KeyPrefix + _encryptionService.EncryptText(licenceKey);
    }

    private string? Unprotect(string? storedValue)
    {
        if (string.IsNullOrWhiteSpace(storedValue))
            return null;

        if (!storedValue.StartsWith(KeyPrefix, StringComparison.Ordinal))
            return storedValue;

        return _encryptionService.DecryptText(storedValue[KeyPrefix.Length..]);
    }

    private sealed class LicenceStateRow
    {
        public DateTime LastHeartbeatUtc { get; set; }
    }

    private sealed class ActivationKeyRow
    {
        public string? ActivationKeyProtected { get; set; }
    }
}
