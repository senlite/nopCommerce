using System.Threading;
using System.Threading.Tasks;

namespace TwinParticles.CheckEngine.Domain.Garage;

public interface IGarageRepository
{
    Task<Garage> GetOrCreateAsync(int customerId, CancellationToken cancellationToken);

    Task<Garage?> GetByCustomerIdAsync(int customerId, CancellationToken cancellationToken);

    Task SaveAsync(Garage garage, CancellationToken cancellationToken);

    Task<bool> DeleteByCustomerIdAsync(int customerId, CancellationToken cancellationToken);
}
