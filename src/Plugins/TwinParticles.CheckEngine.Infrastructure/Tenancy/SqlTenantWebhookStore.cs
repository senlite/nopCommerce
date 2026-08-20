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

public sealed class SqlTenantWebhookStore : ITenantWebhookStore
{
    private readonly INopDataProvider _dataProvider;

    public SqlTenantWebhookStore(INopDataProvider dataProvider)
    {
        _dataProvider = dataProvider;
    }

    public async Task<int> InsertAsync(TenantWebhookSubscription subscription, CancellationToken cancellationToken)
    {
        var id = await _dataProvider.QueryAsync<int>(@"
INSERT INTO TP_CE_TenantWebhook
(TenantId, TargetUrl, SigningSecretProtected, EventTypesCsv, IsActive, CreatedUtc)
VALUES
(@tenantId, @targetUrl, @signingSecretProtected, @eventTypesCsv, @isActive, @createdUtc);
" + CheckEngineSql.SelectInsertedIntId() + @";",
            new DataParameter("tenantId", subscription.TenantId),
            new DataParameter("targetUrl", subscription.TargetUrl),
            new DataParameter("signingSecretProtected", subscription.SigningSecretProtected),
            new DataParameter("eventTypesCsv", subscription.EventTypesCsv),
            new DataParameter("isActive", subscription.IsActive),
            new DataParameter("createdUtc", subscription.CreatedUtc.UtcDateTime));
        return id.FirstOrDefault();
    }

    public async Task<IReadOnlyList<TenantWebhookSubscription>> ListByTenantAsync(int tenantId, CancellationToken cancellationToken)
    {
        var rows = await _dataProvider.QueryAsync<WebhookRow>(@"
SELECT Id, TenantId, TargetUrl, SigningSecretProtected, EventTypesCsv, IsActive, CreatedUtc
FROM TP_CE_TenantWebhook
WHERE TenantId = @tenantId
ORDER BY Id",
            new DataParameter("tenantId", tenantId));
        return rows.Select(Map).ToList();
    }

    public Task SetActiveAsync(int subscriptionId, bool isActive, CancellationToken cancellationToken)
        => _dataProvider.ExecuteNonQueryAsync(@"
UPDATE TP_CE_TenantWebhook SET IsActive = @isActive WHERE Id = @id",
            new DataParameter("id", subscriptionId),
            new DataParameter("isActive", isActive));

    private static TenantWebhookSubscription Map(WebhookRow row)
        => new()
        {
            Id = row.Id,
            TenantId = row.TenantId,
            TargetUrl = row.TargetUrl,
            SigningSecretProtected = row.SigningSecretProtected,
            EventTypesCsv = row.EventTypesCsv,
            IsActive = row.IsActive,
            CreatedUtc = new DateTimeOffset(DateTime.SpecifyKind(row.CreatedUtc, DateTimeKind.Utc))
        };

    private sealed class WebhookRow
    {
        public int Id { get; set; }
        public int TenantId { get; set; }
        public string TargetUrl { get; set; } = string.Empty;
        public string SigningSecretProtected { get; set; } = string.Empty;
        public string EventTypesCsv { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public DateTime CreatedUtc { get; set; }
    }
}
