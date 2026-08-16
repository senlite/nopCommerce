using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using TwinParticles.CheckEngine.Domain.Security;

namespace TwinParticles.CheckEngine.Application.Ai;

public sealed class AiSpendAlertService
{
    private readonly IServiceScopeFactory? _scopeFactory;
    private readonly ConcurrentDictionary<string, byte> _alertedKeys = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentQueue<AiSpendAlert> _recentAlerts = new();

    public AiSpendAlertService(IServiceScopeFactory? scopeFactory = null)
    {
        _scopeFactory = scopeFactory;
    }

    public async Task TryAlertBudgetExceededAsync(
        string featureKey,
        int usage,
        int ceiling,
        bool global,
        CancellationToken cancellationToken)
    {
        var day = DateTime.UtcNow.ToString("yyyy-MM-dd");
        var dedupeKey = $"{day}:{(global ? "global" : featureKey)}";
        if (!_alertedKeys.TryAdd(dedupeKey, 0))
            return;

        var alert = new AiSpendAlert
        {
            FeatureKey = featureKey,
            Usage = usage,
            Ceiling = ceiling,
            Global = global,
            RaisedUtc = DateTime.UtcNow
        };
        EnqueueRecent(alert);

        if (_scopeFactory is null)
            return;

        using var scope = _scopeFactory.CreateScope();
        var auditService = scope.ServiceProvider.GetService<ICheckEngineAuditService>();
        if (auditService is null)
            return;

        await auditService.AppendAsync(
            "system",
            global ? "ai.global_budget_exceeded" : "ai.budget_exceeded",
            "AiFeature",
            featureKey,
            beforeJson: null,
            afterJson: $"{{\"usage\":{usage},\"ceiling\":{ceiling},\"global\":{global.ToString().ToLowerInvariant()}}}",
            cancellationToken);
    }

    public IReadOnlyList<AiSpendAlert> GetRecentAlerts(int take = 20) =>
        _recentAlerts.Reverse().Take(Math.Max(1, take)).ToList();

    private void EnqueueRecent(AiSpendAlert alert)
    {
        _recentAlerts.Enqueue(alert);
        while (_recentAlerts.Count > 50 && _recentAlerts.TryDequeue(out _))
        {
        }
    }
}

public sealed class AiSpendAlert
{
    public string FeatureKey { get; init; } = string.Empty;

    public int Usage { get; init; }

    public int Ceiling { get; init; }

    public bool Global { get; init; }

    public DateTime RaisedUtc { get; init; }
}
