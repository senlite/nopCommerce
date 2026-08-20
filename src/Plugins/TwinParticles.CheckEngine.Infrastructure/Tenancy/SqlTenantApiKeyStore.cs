using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LinqToDB.Data;
using Nop.Data;
using TwinParticles.CheckEngine.Domain.Tenancy;
using TwinParticles.CheckEngine.Infrastructure.Data;

namespace TwinParticles.CheckEngine.Infrastructure.Tenancy;

public sealed class SqlTenantApiKeyStore : ITenantApiKeyStore
{
    private readonly INopDataProvider _dataProvider;

    public SqlTenantApiKeyStore(INopDataProvider dataProvider)
    {
        _dataProvider = dataProvider;
    }

    public async Task<int> InsertAsync(TenantApiKey key, CancellationToken cancellationToken)
    {
        var id = await _dataProvider.QueryAsync<int>(@"
INSERT INTO TP_CE_TenantApiKey
(TenantId, KeyPrefix, KeyHash, ScopesCsv, IsActive, CreatedUtc, LastUsedUtc)
VALUES
(@tenantId, @keyPrefix, @keyHash, @scopesCsv, @isActive, @createdUtc, @lastUsedUtc);
" + CheckEngineSql.SelectInsertedIntId() + @";",
            new DataParameter("tenantId", key.TenantId),
            new DataParameter("keyPrefix", key.KeyPrefix),
            new DataParameter("keyHash", key.KeyHash),
            new DataParameter("scopesCsv", key.ScopesCsv),
            new DataParameter("isActive", key.IsActive),
            new DataParameter("createdUtc", key.CreatedUtc.UtcDateTime),
            new DataParameter("lastUsedUtc", key.LastUsedUtc?.UtcDateTime ?? (object)DBNull.Value));
        return id.FirstOrDefault();
    }

    public async Task<TenantApiKey?> GetByHashAsync(string keyHash, CancellationToken cancellationToken)
    {
        var rows = await _dataProvider.QueryAsync<ApiKeyRow>(@"
SELECT Id, TenantId, KeyPrefix, KeyHash, ScopesCsv, IsActive, CreatedUtc, LastUsedUtc
FROM TP_CE_TenantApiKey
WHERE KeyHash = @keyHash",
            new DataParameter("keyHash", keyHash));
        return rows.Select(Map).FirstOrDefault();
    }

    public async Task<IReadOnlyList<TenantApiKey>> ListByTenantAsync(int tenantId, CancellationToken cancellationToken)
    {
        var rows = await _dataProvider.QueryAsync<ApiKeyRow>(@"
SELECT Id, TenantId, KeyPrefix, KeyHash, ScopesCsv, IsActive, CreatedUtc, LastUsedUtc
FROM TP_CE_TenantApiKey
WHERE TenantId = @tenantId
ORDER BY Id",
            new DataParameter("tenantId", tenantId));
        return rows.Select(Map).ToList();
    }

    public Task SetActiveAsync(int keyId, bool isActive, CancellationToken cancellationToken)
        => _dataProvider.ExecuteNonQueryAsync(@"
UPDATE TP_CE_TenantApiKey SET IsActive = @isActive WHERE Id = @id",
            new DataParameter("id", keyId),
            new DataParameter("isActive", isActive));

    public Task TouchLastUsedAsync(int keyId, DateTimeOffset utc, CancellationToken cancellationToken)
        => _dataProvider.ExecuteNonQueryAsync(@"
UPDATE TP_CE_TenantApiKey SET LastUsedUtc = @lastUsedUtc WHERE Id = @id",
            new DataParameter("id", keyId),
            new DataParameter("lastUsedUtc", utc.UtcDateTime));

    private static TenantApiKey Map(ApiKeyRow row)
        => new()
        {
            Id = row.Id,
            TenantId = row.TenantId,
            KeyPrefix = row.KeyPrefix,
            KeyHash = row.KeyHash,
            ScopesCsv = row.ScopesCsv,
            IsActive = row.IsActive,
            CreatedUtc = new DateTimeOffset(DateTime.SpecifyKind(row.CreatedUtc, DateTimeKind.Utc)),
            LastUsedUtc = row.LastUsedUtc is DateTime last
                ? new DateTimeOffset(DateTime.SpecifyKind(last, DateTimeKind.Utc))
                : null
        };

    private sealed class ApiKeyRow
    {
        public int Id { get; set; }
        public int TenantId { get; set; }
        public string KeyPrefix { get; set; } = string.Empty;
        public string KeyHash { get; set; } = string.Empty;
        public string ScopesCsv { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public DateTime CreatedUtc { get; set; }
        public DateTime? LastUsedUtc { get; set; }
    }
}
