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

public sealed class SqlTenantRegistry : ITenantRegistry
{
    private readonly INopDataProvider _dataProvider;

    public SqlTenantRegistry(INopDataProvider dataProvider)
    {
        _dataProvider = dataProvider;
    }

    public async Task<Tenant?> GetByIdAsync(int tenantId, CancellationToken cancellationToken)
    {
        var rows = await _dataProvider.QueryAsync<TenantRow>(@"
SELECT Id, Slug, DisplayName, StatusId, IsolationModeId, ConnectionName, HostnamesCsv, IsSelfHosted, CreatedUtc, UpdatedUtc
FROM TP_CE_Tenant
WHERE Id = @id",
            new DataParameter("id", tenantId));
        return rows.Select(Map).FirstOrDefault();
    }

    public async Task<Tenant?> GetBySlugAsync(string slug, CancellationToken cancellationToken)
    {
        var rows = await _dataProvider.QueryAsync<TenantRow>(@"
SELECT Id, Slug, DisplayName, StatusId, IsolationModeId, ConnectionName, HostnamesCsv, IsSelfHosted, CreatedUtc, UpdatedUtc
FROM TP_CE_Tenant
WHERE Slug = @slug",
            new DataParameter("slug", slug));
        return rows.Select(Map).FirstOrDefault();
    }

    public async Task<Tenant?> GetByHostnameAsync(string hostname, CancellationToken cancellationToken)
    {
        var all = await ListAsync(cancellationToken);
        return all.FirstOrDefault(tenant =>
            tenant.HostnamesCsv
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Any(host => string.Equals(host, hostname, StringComparison.OrdinalIgnoreCase)));
    }

    public async Task<IReadOnlyList<Tenant>> ListAsync(CancellationToken cancellationToken)
    {
        var rows = await _dataProvider.QueryAsync<TenantRow>(@"
SELECT Id, Slug, DisplayName, StatusId, IsolationModeId, ConnectionName, HostnamesCsv, IsSelfHosted, CreatedUtc, UpdatedUtc
FROM TP_CE_Tenant
ORDER BY Id");
        return rows.Select(Map).ToList();
    }

    public async Task<int> InsertAsync(Tenant tenant, CancellationToken cancellationToken)
    {
        var id = await _dataProvider.QueryAsync<int>(@"
INSERT INTO TP_CE_Tenant
(Slug, DisplayName, StatusId, IsolationModeId, ConnectionName, HostnamesCsv, IsSelfHosted, CreatedUtc, UpdatedUtc)
VALUES
(@slug, @displayName, @statusId, @isolationModeId, @connectionName, @hostnamesCsv, @isSelfHosted, @createdUtc, @updatedUtc);
" + CheckEngineSql.SelectInsertedIntId() + @";",
            new DataParameter("slug", tenant.Slug),
            new DataParameter("displayName", tenant.DisplayName),
            new DataParameter("statusId", (int)tenant.Status),
            new DataParameter("isolationModeId", (int)tenant.IsolationMode),
            new DataParameter("connectionName", tenant.ConnectionName ?? string.Empty),
            new DataParameter("hostnamesCsv", tenant.HostnamesCsv ?? string.Empty),
            new DataParameter("isSelfHosted", tenant.IsSelfHosted),
            new DataParameter("createdUtc", tenant.CreatedUtc.UtcDateTime),
            new DataParameter("updatedUtc", tenant.UpdatedUtc.UtcDateTime));
        return id.FirstOrDefault();
    }

    public Task UpdateAsync(Tenant tenant, CancellationToken cancellationToken)
        => _dataProvider.ExecuteNonQueryAsync(@"
UPDATE TP_CE_Tenant
SET DisplayName = @displayName,
    StatusId = @statusId,
    IsolationModeId = @isolationModeId,
    ConnectionName = @connectionName,
    HostnamesCsv = @hostnamesCsv,
    UpdatedUtc = @updatedUtc
WHERE Id = @id",
            new DataParameter("id", tenant.Id),
            new DataParameter("displayName", tenant.DisplayName),
            new DataParameter("statusId", (int)tenant.Status),
            new DataParameter("isolationModeId", (int)tenant.IsolationMode),
            new DataParameter("connectionName", tenant.ConnectionName ?? string.Empty),
            new DataParameter("hostnamesCsv", tenant.HostnamesCsv ?? string.Empty),
            new DataParameter("updatedUtc", tenant.UpdatedUtc.UtcDateTime));

    private static Tenant Map(TenantRow row)
        => new()
        {
            Id = row.Id,
            Slug = row.Slug,
            DisplayName = row.DisplayName,
            Status = (TenantStatus)row.StatusId,
            IsolationMode = (TenantIsolationMode)row.IsolationModeId,
            ConnectionName = row.ConnectionName ?? string.Empty,
            HostnamesCsv = row.HostnamesCsv ?? string.Empty,
            IsSelfHosted = row.IsSelfHosted,
            CreatedUtc = new DateTimeOffset(DateTime.SpecifyKind(row.CreatedUtc, DateTimeKind.Utc)),
            UpdatedUtc = new DateTimeOffset(DateTime.SpecifyKind(row.UpdatedUtc, DateTimeKind.Utc))
        };

    private sealed class TenantRow
    {
        public int Id { get; set; }
        public string Slug { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public int StatusId { get; set; }
        public int IsolationModeId { get; set; }
        public string ConnectionName { get; set; } = string.Empty;
        public string HostnamesCsv { get; set; } = string.Empty;
        public bool IsSelfHosted { get; set; }
        public DateTime CreatedUtc { get; set; }
        public DateTime UpdatedUtc { get; set; }
    }
}
