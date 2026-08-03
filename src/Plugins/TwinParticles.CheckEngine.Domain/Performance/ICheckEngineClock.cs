using System;

namespace TwinParticles.CheckEngine.Domain.Performance;

public interface ICheckEngineClock
{
    DateTimeOffset UtcNow { get; }
}
