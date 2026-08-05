using System;
using System.Threading;
using System.Threading.Tasks;
using TwinParticles.CheckEngine.Domain.Licensing;
using TwinParticles.CheckEngine.Domain.Performance;

namespace TwinParticles.CheckEngine.Application.Licensing;

public sealed class DefaultLicenceService : ILicenceService
{
    private readonly ICheckEngineClock _clock;
    private readonly ILicenceStateStore _stateStore;

    public DefaultLicenceService(ILicenceStateStore stateStore, ICheckEngineClock clock)
    {
        _stateStore = stateStore;
        _clock = clock;
    }

    public async Task<LicenceStatus> GetStatusAsync(CancellationToken cancellationToken)
    {
        var lastHeartbeatUtc = await _stateStore.GetLastHeartbeatUtcAsync(cancellationToken);
        return new LicenceStatus
        {
            IsActive = lastHeartbeatUtc.HasValue,
            State = lastHeartbeatUtc.HasValue ? "active" : "inactive",
            LastHeartbeatUtc = lastHeartbeatUtc,
            ReasonCode = lastHeartbeatUtc.HasValue ? null : "licence.not_activated"
        };
    }

    public async Task<LicenceStatus> ActivateAsync(string licenceKey, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(licenceKey))
        {
            return new LicenceStatus
            {
                IsActive = false,
                State = "invalid",
                ReasonCode = "licence.invalid_key"
            };
        }

        await _stateStore.SetLastHeartbeatUtcAsync(_clock.UtcNow, cancellationToken);
        return await GetStatusAsync(cancellationToken);
    }

    public async Task<LicenceStatus> HeartbeatAsync(CancellationToken cancellationToken)
    {
        await _stateStore.SetLastHeartbeatUtcAsync(_clock.UtcNow, cancellationToken);
        return await GetStatusAsync(cancellationToken);
    }
}
