using System;
using FluentAssertions;
using NUnit.Framework;
using TwinParticles.CheckEngine.Domain.Performance;
using TwinParticles.CheckEngine.Infrastructure.Vehicle.VinDecoders;

namespace TwinParticles.CheckEngine.Tests.Architecture;

[TestFixture]
public class InMemoryVinDecodeRateLimiterTests
{
    [Test]
    public void TryAcquire_Should_Block_After_Window_Limit_And_Reset_On_Next_Window()
    {
        var clock = new MutableClock(new DateTimeOffset(2026, 8, 3, 12, 0, 0, TimeSpan.Zero));
        var limiter = new InMemoryVinDecodeRateLimiter(clock);

        for (var i = 0; i < 30; i++)
        {
            limiter.TryAcquire("vin:ip:127.0.0.1", out _).Should().BeTrue();
        }

        limiter.TryAcquire("vin:ip:127.0.0.1", out var retryAfterSeconds).Should().BeFalse();
        retryAfterSeconds.Should().BeGreaterThan(0);

        clock.UtcNow = clock.UtcNow.AddMinutes(1).AddSeconds(1);

        limiter.TryAcquire("vin:ip:127.0.0.1", out _).Should().BeTrue();
    }

    private sealed class MutableClock : ICheckEngineClock
    {
        public MutableClock(DateTimeOffset utcNow)
        {
            UtcNow = utcNow;
        }

        public DateTimeOffset UtcNow { get; set; }
    }
}
