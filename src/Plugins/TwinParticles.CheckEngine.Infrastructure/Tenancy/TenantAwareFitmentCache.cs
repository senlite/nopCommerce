using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using TwinParticles.CheckEngine.Domain.Fitment;
using TwinParticles.CheckEngine.Domain.Tenancy;

namespace TwinParticles.CheckEngine.Infrastructure.Tenancy;

/// <summary>FR-1312: fitment verdict cache keys are tenant-qualified.</summary>
public sealed class TenantAwareFitmentCache : IFitmentCache
{
    private readonly ConcurrentDictionary<string, FitmentEvaluationResult> _cache = new(StringComparer.Ordinal);
    private readonly ITenantAccessor _accessor;

    public TenantAwareFitmentCache(ITenantAccessor accessor)
    {
        _accessor = accessor;
    }

    public Task<FitmentEvaluationResult?> GetAsync(FitmentEvaluationContext context, CancellationToken cancellationToken)
    {
        _cache.TryGetValue(GetKey(context), out var result);
        return Task.FromResult<FitmentEvaluationResult?>(result);
    }

    public Task SetAsync(FitmentEvaluationContext context, FitmentEvaluationResult result, CancellationToken cancellationToken)
    {
        _cache[GetKey(context)] = result;
        return Task.CompletedTask;
    }

    public Task InvalidateAsync(int productId, int vehicleConfigurationId, CancellationToken cancellationToken)
    {
        var innerPrefix = $"{productId}:{vehicleConfigurationId}:";
        foreach (var key in _cache.Keys)
        {
            var inner = InnerKeyFromQualified(key);
            if (inner.StartsWith(innerPrefix, StringComparison.Ordinal))
                _cache.TryRemove(key, out _);
        }

        return Task.CompletedTask;
    }

    public string GetKey(FitmentEvaluationContext context)
        => TenantCacheKey.Qualify(_accessor.Current.TenantId, InnerKey(context));

    private static string InnerKey(FitmentEvaluationContext context)
        => string.Join(':',
            context.ProductId,
            context.VehicleConfigurationId,
            context.ProductionYear?.ToString() ?? "-",
            Normalize(context.SteeringSide),
            Normalize(context.MarketRegion),
            Normalize(context.DriveType),
            Normalize(context.TransmissionType));

    private static string InnerKeyFromQualified(string qualified)
    {
        var prefix = TenantCacheKey.Prefix;
        var secondColon = qualified.IndexOf(':', prefix.Length);
        return secondColon < 0 ? qualified : qualified[(secondColon + 1)..];
    }

    private static string Normalize(string? value)
        => string.IsNullOrWhiteSpace(value) ? "-" : value.Trim().ToUpperInvariant();
}
