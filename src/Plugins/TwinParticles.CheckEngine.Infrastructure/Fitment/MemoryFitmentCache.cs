using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using TwinParticles.CheckEngine.Domain.Fitment;

namespace TwinParticles.CheckEngine.Infrastructure.Fitment;

public sealed class MemoryFitmentCache : IFitmentCache
{
    private readonly ConcurrentDictionary<string, FitmentEvaluationResult> _cache = new(StringComparer.Ordinal);

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
        var prefix = GetPrefix(productId, vehicleConfigurationId);
        foreach (var key in _cache.Keys)
        {
            if (key.StartsWith(prefix, StringComparison.Ordinal))
                _cache.TryRemove(key, out _);
        }

        return Task.CompletedTask;
    }

    private static string GetPrefix(int productId, int vehicleConfigurationId)
        => $"{productId}:{vehicleConfigurationId}:";

    private static string GetKey(FitmentEvaluationContext context)
        => string.Join(':',
            context.ProductId,
            context.VehicleConfigurationId,
            context.ProductionYear?.ToString() ?? "-",
            Normalize(context.SteeringSide),
            Normalize(context.MarketRegion),
            Normalize(context.DriveType),
            Normalize(context.TransmissionType));

    private static string Normalize(string? value)
        => string.IsNullOrWhiteSpace(value) ? "-" : value.Trim().ToUpperInvariant();
}
