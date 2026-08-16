using System;
using System.Threading;
using System.Threading.Tasks;
using TwinParticles.CheckEngine.Domain.Licensing;

namespace TwinParticles.CheckEngine.Infrastructure.Licensing;

public sealed class InMemoryLicenceStateStore : ILicenceStateStore
{
    private readonly object _syncRoot = new();
    private DateTimeOffset? _lastHeartbeatUtc;
    private string? _activationKey;

    public Task<DateTimeOffset?> GetLastHeartbeatUtcAsync(CancellationToken cancellationToken)
    {
        lock (_syncRoot)
        {
            return Task.FromResult(_lastHeartbeatUtc);
        }
    }

    public Task SetLastHeartbeatUtcAsync(DateTimeOffset heartbeatUtc, CancellationToken cancellationToken)
    {
        lock (_syncRoot)
        {
            _lastHeartbeatUtc = heartbeatUtc;
        }

        return Task.CompletedTask;
    }

    public Task<string?> GetActivationKeyAsync(CancellationToken cancellationToken)
    {
        lock (_syncRoot)
        {
            return Task.FromResult(_activationKey);
        }
    }

    public Task SetActivationKeyAsync(string licenceKey, CancellationToken cancellationToken)
    {
        lock (_syncRoot)
        {
            _activationKey = licenceKey;
        }

        return Task.CompletedTask;
    }
}
