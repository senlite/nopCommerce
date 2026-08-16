using System;
using System.Threading;
using System.Threading.Tasks;

namespace TwinParticles.CheckEngine.Domain.Licensing;

public interface ILicenceStateStore
{
    Task<DateTimeOffset?> GetLastHeartbeatUtcAsync(CancellationToken cancellationToken);

    Task SetLastHeartbeatUtcAsync(DateTimeOffset heartbeatUtc, CancellationToken cancellationToken);

    Task<string?> GetActivationKeyAsync(CancellationToken cancellationToken);

    Task SetActivationKeyAsync(string licenceKey, CancellationToken cancellationToken);
}
