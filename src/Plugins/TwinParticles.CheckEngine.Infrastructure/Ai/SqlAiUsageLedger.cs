using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LinqToDB.Data;
using Nop.Data;
using TwinParticles.CheckEngine.Domain.Ai;

namespace TwinParticles.CheckEngine.Infrastructure.Ai;

public sealed class SqlAiUsageLedger : IAiUsageLedger
{
    private readonly INopDataProvider _dataProvider;

    public SqlAiUsageLedger(INopDataProvider dataProvider)
    {
        _dataProvider = dataProvider;
    }

    public async Task RecordAsync(string featureKey, int tokenUsage, CancellationToken cancellationToken)
    {
        var day = DateTime.UtcNow.Date;
        await _dataProvider.ExecuteNonQueryAsync(@"
MERGE TP_CE_AiUsageDaily AS target
USING (SELECT @featureKey AS FeatureKey, @usageDay AS UsageDay) AS source
ON target.FeatureKey = source.FeatureKey AND target.UsageDay = source.UsageDay
WHEN MATCHED THEN UPDATE SET TokenUsage = target.TokenUsage + @tokenUsage
WHEN NOT MATCHED THEN INSERT (FeatureKey, UsageDay, TokenUsage) VALUES (@featureKey, @usageDay, @tokenUsage);",
            new DataParameter("featureKey", featureKey),
            new DataParameter("usageDay", day),
            new DataParameter("tokenUsage", Math.Max(0, tokenUsage)));
    }

    public async Task<int> GetDailyUsageAsync(string featureKey, CancellationToken cancellationToken)
    {
        var day = DateTime.UtcNow.Date;
        var rows = await _dataProvider.QueryAsync<int>(@"
SELECT TokenUsage FROM TP_CE_AiUsageDaily
WHERE FeatureKey = @featureKey AND UsageDay = @usageDay",
            new DataParameter("featureKey", featureKey),
            new DataParameter("usageDay", day));

        return rows.FirstOrDefault();
    }

    public async Task<bool> IsCeilingExceededAsync(string featureKey, int ceiling, CancellationToken cancellationToken)
    {
        var usage = await GetDailyUsageAsync(featureKey, cancellationToken);
        return usage >= ceiling;
    }
}
