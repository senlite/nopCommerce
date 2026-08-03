using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using TwinParticles.CheckEngine.Domain.Fitment;

namespace TwinParticles.CheckEngine.Infrastructure.Fitment;

public sealed class MemoryFitmentCache : IFitmentCache
{
    private readonly ConcurrentDictionary<string, FitmentEvaluationResult> _cache = new(StringComparer.Ordinal);

    public Task<FitmentEvaluationResult?> GetAsync(int productId, int vehicleConfigurationId, CancellationToken cancellationToken)
    {
        _cache.TryGetValue(GetKey(productId, vehicleConfigurationId), out var result);
        return Task.FromResult<FitmentEvaluationResult?>(result);
    }

    public Task SetAsync(int productId, int vehicleConfigurationId, FitmentEvaluationResult result, CancellationToken cancellationToken)
    {
        _cache[GetKey(productId, vehicleConfigurationId)] = result;
        return Task.CompletedTask;
    }

    public Task InvalidateAsync(int productId, int vehicleConfigurationId, CancellationToken cancellationToken)
    {
        _cache.TryRemove(GetKey(productId, vehicleConfigurationId), out _);
        return Task.CompletedTask;
    }

    private static string GetKey(int productId, int vehicleConfigurationId) => $"{productId}:{vehicleConfigurationId}";
}
