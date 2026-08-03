using System;

namespace TwinParticles.CheckEngine.Application.Common.Performance;

public interface ICheckEngineClock
{
    DateTimeOffset UtcNow { get; }
}
