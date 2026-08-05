using System;

namespace TwinParticles.CheckEngine.Domain.Licensing;

public sealed class LicenceStatus
{
    public bool IsActive { get; init; }

    public string State { get; init; } = "inactive";

    public DateTimeOffset? LastHeartbeatUtc { get; init; }

    public string? ReasonCode { get; init; }
}
