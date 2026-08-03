using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TwinParticles.CheckEngine.Domain.Vehicle.Aliases;

namespace TwinParticles.CheckEngine.Infrastructure.Vehicle.Aliases;

public sealed class MemoryVehicleAliasCache : IVehicleAliasCache
{
    private readonly TimeSpan _cacheDuration;
    private readonly ConcurrentDictionary<string, CacheEntry> _store = new(StringComparer.OrdinalIgnoreCase);

    public MemoryVehicleAliasCache() : this(TimeSpan.FromMinutes(10))
    {
    }

    public MemoryVehicleAliasCache(TimeSpan cacheDuration)
    {
        _cacheDuration = cacheDuration;
    }

    public Task<IReadOnlyList<VehicleAliasSearchItem>?> GetAsync(string term, string locale, int take, CancellationToken cancellationToken)
    {
        var key = BuildKey(term, locale, take);
        if (!_store.TryGetValue(key, out var entry))
            return Task.FromResult<IReadOnlyList<VehicleAliasSearchItem>?>(null);

        if (entry.ExpiresAtUtc < DateTimeOffset.UtcNow)
        {
            _store.TryRemove(key, out _);
            return Task.FromResult<IReadOnlyList<VehicleAliasSearchItem>?>(null);
        }

        return Task.FromResult<IReadOnlyList<VehicleAliasSearchItem>?>(entry.Items);
    }

    public Task SetAsync(string term, string locale, int take, IReadOnlyList<VehicleAliasSearchItem> items, CancellationToken cancellationToken)
    {
        var key = BuildKey(term, locale, take);
        _store[key] = new CacheEntry(items, DateTimeOffset.UtcNow.Add(_cacheDuration));
        return Task.CompletedTask;
    }

    public Task InvalidateAsync(string locale, CancellationToken cancellationToken)
    {
        foreach (var key in _store.Keys)
        {
            if (key.Contains($"|{locale}|", StringComparison.OrdinalIgnoreCase))
                _store.TryRemove(key, out _);
        }

        return Task.CompletedTask;
    }

    private static string BuildKey(string term, string locale, int take)
        => $"{term.Trim().ToLowerInvariant()}|{locale.Trim().ToLowerInvariant()}|{take}";

    private sealed record CacheEntry(IReadOnlyList<VehicleAliasSearchItem> Items, DateTimeOffset ExpiresAtUtc);
}
