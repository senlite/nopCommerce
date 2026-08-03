using System;
using TwinParticles.CheckEngine.Domain.Performance;

namespace TwinParticles.CheckEngine.Infrastructure.Performance;

public sealed class SystemCheckEngineClock : ICheckEngineClock
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
