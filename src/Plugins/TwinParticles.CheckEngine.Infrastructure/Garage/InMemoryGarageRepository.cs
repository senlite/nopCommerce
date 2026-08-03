using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using TwinParticles.CheckEngine.Domain.Garage;

namespace TwinParticles.CheckEngine.Infrastructure.Garage;

public sealed class InMemoryGarageRepository : IGarageRepository
{
    private readonly ConcurrentDictionary<int, Domain.Garage.Garage> _garagesByCustomerId = new();

    public Task<Domain.Garage.Garage> GetOrCreateAsync(int customerId, CancellationToken cancellationToken)
    {
        var garage = _garagesByCustomerId.GetOrAdd(customerId, id => new Domain.Garage.Garage
        {
            Id = id,
            CustomerId = id,
            CreatedUtc = DateTime.UtcNow,
            UpdatedUtc = DateTime.UtcNow
        });

        return Task.FromResult(garage);
    }

    public Task<Domain.Garage.Garage?> GetByCustomerIdAsync(int customerId, CancellationToken cancellationToken)
    {
        _garagesByCustomerId.TryGetValue(customerId, out var garage);
        return Task.FromResult(garage);
    }

    public Task SaveAsync(Domain.Garage.Garage garage, CancellationToken cancellationToken)
    {
        garage.UpdatedUtc = DateTime.UtcNow;
        _garagesByCustomerId[garage.CustomerId] = garage;
        return Task.CompletedTask;
    }
}
