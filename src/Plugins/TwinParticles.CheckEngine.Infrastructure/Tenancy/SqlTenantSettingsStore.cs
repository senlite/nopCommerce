using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LinqToDB.Data;
using Nop.Data;
using TwinParticles.CheckEngine.Domain.Tenancy;

namespace TwinParticles.CheckEngine.Infrastructure.Tenancy;

public sealed class SqlTenantSettingsStore : ITenantSettingsStore
{
    private readonly INopDataProvider _dataProvider;

    public SqlTenantSettingsStore(INopDataProvider dataProvider)
    {
        _dataProvider = dataProvider;
    }

    public async Task<string?> GetAsync(int tenantId, string key, CancellationToken cancellationToken)
    {
        var rows = await _dataProvider.QueryAsync<SettingRow>(@"
SELECT SettingValue
FROM TP_CE_TenantSetting
WHERE TenantId = @tenantId AND SettingKey = @settingKey",
            new DataParameter("tenantId", tenantId),
            new DataParameter("settingKey", key));
        return rows.Select(row => row.SettingValue).FirstOrDefault();
    }

    public async Task UpsertAsync(TenantSetting setting, CancellationToken cancellationToken)
    {
        var updated = await _dataProvider.ExecuteNonQueryAsync(@"
UPDATE TP_CE_TenantSetting
SET SettingValue = @settingValue
WHERE TenantId = @tenantId AND SettingKey = @settingKey",
            new DataParameter("tenantId", setting.TenantId),
            new DataParameter("settingKey", setting.Key),
            new DataParameter("settingValue", setting.Value));
        if (updated > 0)
            return;

        await _dataProvider.ExecuteNonQueryAsync(@"
INSERT INTO TP_CE_TenantSetting (TenantId, SettingKey, SettingValue)
VALUES (@tenantId, @settingKey, @settingValue)",
            new DataParameter("tenantId", setting.TenantId),
            new DataParameter("settingKey", setting.Key),
            new DataParameter("settingValue", setting.Value));
    }

    private sealed class SettingRow
    {
        public string SettingValue { get; set; } = string.Empty;
    }
}
