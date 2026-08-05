using System.Threading;
using System.Threading.Tasks;

namespace TwinParticles.CheckEngine.Domain.Licensing;

public interface ILicenceService
{
    Task<LicenceStatus> GetStatusAsync(CancellationToken cancellationToken);

    Task<LicenceStatus> ActivateAsync(string licenceKey, CancellationToken cancellationToken);

    Task<LicenceStatus> HeartbeatAsync(CancellationToken cancellationToken);
}
