using System.Threading;
using System.Threading.Tasks;

namespace TwinParticles.CheckEngine.Domain.Garage;

public interface IGarageGuestStore
{
    Task<GarageGuestPayload?> GetAsync(string guestKey, CancellationToken cancellationToken);

    Task SetAsync(string guestKey, GarageGuestPayload payload, CancellationToken cancellationToken);

    Task RemoveAsync(string guestKey, CancellationToken cancellationToken);
}
