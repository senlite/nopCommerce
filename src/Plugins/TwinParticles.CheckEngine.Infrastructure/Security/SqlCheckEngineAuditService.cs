using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using LinqToDB.Data;
using Nop.Core.Domain.Security;
using Nop.Data;
using TwinParticles.CheckEngine.Domain.Security;
using TwinParticles.CheckEngine.Infrastructure.Data;

namespace TwinParticles.CheckEngine.Infrastructure.Security;

public sealed class SqlCheckEngineAuditService : ICheckEngineAuditService, IAuditIntegrityService
{
    private const string ZeroHash = "0000000000000000000000000000000000000000000000000000000000000000";
    private const string LockResource = "TP_CE_AuditEvent.HashChain";
    private readonly INopDataProvider _dataProvider;
    private readonly string _chainSecret;

    public SqlCheckEngineAuditService(INopDataProvider dataProvider, SecuritySettings securitySettings)
    {
        _dataProvider = dataProvider;
        _chainSecret = securitySettings.EncryptionKey;
    }

    public async Task AppendAsync(
        string actor,
        string action,
        string entityType,
        string entityId,
        string? beforeJson,
        string? afterJson,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await AcquireLockAsync();
        try
        {
            await BackfillLegacyHashesAsync();

            var createdUtc = DateTime.UtcNow;
            var previousHash = await GetTipHashAsync() ?? ZeroHash;
            var entryHash = ComputeEntryHash(
                previousHash,
                actor ?? string.Empty,
                action ?? string.Empty,
                entityType ?? string.Empty,
                entityId ?? string.Empty,
                beforeJson,
                afterJson,
                createdUtc);

            await _dataProvider.ExecuteNonQueryAsync(
                @"INSERT INTO TP_CE_AuditEvent
    (Actor, Action, EntityType, EntityId, BeforeJson, AfterJson, CreatedUtc, PreviousHash, EntryHash)
VALUES
    (@actor, @action, @entityType, @entityId, @beforeJson, @afterJson, @createdUtc, @previousHash, @entryHash)",
                new DataParameter("actor", actor ?? string.Empty),
                new DataParameter("action", action ?? string.Empty),
                new DataParameter("entityType", entityType ?? string.Empty),
                new DataParameter("entityId", entityId ?? string.Empty),
                new DataParameter("beforeJson", (object?)beforeJson ?? DBNull.Value),
                new DataParameter("afterJson", (object?)afterJson ?? DBNull.Value),
                new DataParameter("createdUtc", createdUtc),
                new DataParameter("previousHash", previousHash),
                new DataParameter("entryHash", entryHash));
        }
        finally
        {
            await ReleaseLockAsync();
        }
    }

    public async Task<AuditIntegrityResult> VerifyAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var rows = (await _dataProvider.QueryAsync<AuditRow>(
            @"SELECT Id, Actor, Action, EntityType, EntityId, BeforeJson, AfterJson, CreatedUtc, PreviousHash, EntryHash
FROM TP_CE_AuditEvent
ORDER BY Id")).ToList();

        var expectedPrevious = (await GetAnchorHashAsync()) ?? ZeroHash;
        var invalid = 0;
        foreach (var row in rows)
        {
            if (string.IsNullOrWhiteSpace(row.EntryHash) ||
                !string.Equals(row.PreviousHash, expectedPrevious, StringComparison.OrdinalIgnoreCase) ||
                !string.Equals(
                    row.EntryHash,
                    ComputeEntryHash(
                        row.PreviousHash ?? ZeroHash,
                        row.Actor,
                        row.Action,
                        row.EntityType,
                        row.EntityId,
                        row.BeforeJson,
                        row.AfterJson,
                        row.CreatedUtc),
                    StringComparison.OrdinalIgnoreCase))
            {
                invalid++;
            }

            expectedPrevious = row.EntryHash ?? expectedPrevious;
        }

        return new AuditIntegrityResult
        {
            IsValid = invalid == 0,
            CheckedEntries = rows.Count,
            InvalidEntries = invalid
        };
    }

    public async Task<int> PruneAsync(DateTime retainFromUtc, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await AcquireLockAsync();
        try
        {
            var tip = (await _dataProvider.QueryAsync<PruneTipRow>(
                CheckEngineSql.SelectTop(
                    1,
                    "Id, EntryHash",
                    "FROM TP_CE_AuditEvent WHERE CreatedUtc<@retainFromUtc AND EntryHash IS NOT NULL ORDER BY Id DESC"),
                new DataParameter("retainFromUtc", retainFromUtc))).FirstOrDefault();

            if (tip is null || tip.Id <= 0)
                return 0;

            var updated = await _dataProvider.ExecuteNonQueryAsync(
                @"UPDATE TP_CE_AuditChainAnchor
SET LastPrunedAuditEventId=@pruneId, LastPrunedHash=@pruneHash, PrunedUtc=@prunedUtc
WHERE Id=1",
                new DataParameter("pruneId", tip.Id),
                new DataParameter("pruneHash", tip.EntryHash),
                new DataParameter("prunedUtc", DateTime.UtcNow));

            if (updated == 0)
            {
                await _dataProvider.ExecuteNonQueryAsync(
                    @"INSERT INTO TP_CE_AuditChainAnchor (Id, LastPrunedAuditEventId, LastPrunedHash, PrunedUtc)
VALUES (1, @pruneId, @pruneHash, @prunedUtc)",
                    new DataParameter("pruneId", tip.Id),
                    new DataParameter("pruneHash", tip.EntryHash),
                    new DataParameter("prunedUtc", DateTime.UtcNow));
            }

            return await _dataProvider.ExecuteNonQueryAsync(
                "DELETE FROM TP_CE_AuditEvent WHERE Id<=@pruneId",
                new DataParameter("pruneId", tip.Id));
        }
        finally
        {
            await ReleaseLockAsync();
        }
    }

    private async Task BackfillLegacyHashesAsync()
    {
        var legacy = (await _dataProvider.QueryAsync<AuditRow>(
            @"SELECT Id, Actor, Action, EntityType, EntityId, BeforeJson, AfterJson, CreatedUtc, PreviousHash, EntryHash
FROM TP_CE_AuditEvent
WHERE EntryHash IS NULL
ORDER BY Id")).ToList();

        if (legacy.Count == 0)
            return;

        var previousHash = await GetTipHashAsync() ?? (await GetAnchorHashAsync()) ?? ZeroHash;
        foreach (var row in legacy)
        {
            var entryHash = ComputeEntryHash(
                previousHash,
                row.Actor,
                row.Action,
                row.EntityType,
                row.EntityId,
                row.BeforeJson,
                row.AfterJson,
                row.CreatedUtc);

            await _dataProvider.ExecuteNonQueryAsync(
                "UPDATE TP_CE_AuditEvent SET PreviousHash=@previousHash, EntryHash=@entryHash WHERE Id=@id",
                new DataParameter("previousHash", previousHash),
                new DataParameter("entryHash", entryHash),
                new DataParameter("id", row.Id));

            previousHash = entryHash;
        }
    }

    private async Task<string?> GetTipHashAsync()
    {
        var rows = await _dataProvider.QueryAsync<HashRow>(
            CheckEngineSql.SelectTop(
                1,
                "EntryHash",
                "FROM TP_CE_AuditEvent WHERE EntryHash IS NOT NULL ORDER BY Id DESC"));
        return rows.Select(row => row.EntryHash).FirstOrDefault();
    }

    private async Task<string?> GetAnchorHashAsync()
    {
        var rows = await _dataProvider.QueryAsync<HashRow>(
            "SELECT LastPrunedHash AS EntryHash FROM TP_CE_AuditChainAnchor WHERE Id=1");
        return rows.Select(row => row.EntryHash).FirstOrDefault();
    }

    private string ComputeEntryHash(
        string previousHash,
        string actor,
        string action,
        string entityType,
        string entityId,
        string? beforeJson,
        string? afterJson,
        DateTime createdUtc)
    {
        // HASHBYTES('SHA2_256', CONCAT(..., NCHAR(31), ...)) equivalent, computed in-process so MySQL
        // does not need HASHBYTES. sp_getapplock remains available via CheckEngineSql for SQL Server.
        var payload = string.Join(
            '\u001f',
            _chainSecret,
            previousHash,
            actor,
            action,
            entityType,
            entityId,
            beforeJson ?? string.Empty,
            afterJson ?? string.Empty,
            createdUtc.ToString("yyyy-MM-ddTHH:mm:ss.FFFFFFF"));

        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(payload)));
    }

    private Task AcquireLockAsync()
        => _dataProvider.ExecuteNonQueryAsync(
            CheckEngineSql.AcquireSessionLock(),
            new DataParameter("resource", LockResource));

    private Task ReleaseLockAsync()
        => _dataProvider.ExecuteNonQueryAsync(
            CheckEngineSql.ReleaseSessionLock(),
            new DataParameter("resource", LockResource));

    private sealed class AuditRow
    {
        public int Id { get; set; }
        public string Actor { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
        public string EntityType { get; set; } = string.Empty;
        public string EntityId { get; set; } = string.Empty;
        public string? BeforeJson { get; set; }
        public string? AfterJson { get; set; }
        public DateTime CreatedUtc { get; set; }
        public string? PreviousHash { get; set; }
        public string? EntryHash { get; set; }
    }

    private sealed class HashRow
    {
        public string? EntryHash { get; set; }
    }

    private sealed class PruneTipRow
    {
        public int Id { get; set; }
        public string EntryHash { get; set; } = string.Empty;
    }
}
