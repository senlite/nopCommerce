using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using TwinParticles.CheckEngine.Domain.Garage;

namespace TwinParticles.CheckEngine.Infrastructure.Garage;

public sealed class InMemoryGarageGuestStore : IGarageGuestStore
{
    private readonly ConcurrentDictionary<string, GarageGuestPayload> _payloads = new();

    public Task<GarageGuestPayload?> GetAsync(string guestKey, CancellationToken cancellationToken)
    {
        _payloads.TryGetValue(guestKey, out var payload);
        return Task.FromResult(payload);
    }

    public Task SetAsync(string guestKey, GarageGuestPayload payload, CancellationToken cancellationToken)
    {
        _payloads[guestKey] = payload;
        return Task.CompletedTask;
    }

    public Task RemoveAsync(string guestKey, CancellationToken cancellationToken)
    {
        _payloads.TryRemove(guestKey, out _);
        return Task.CompletedTask;
    }
}
