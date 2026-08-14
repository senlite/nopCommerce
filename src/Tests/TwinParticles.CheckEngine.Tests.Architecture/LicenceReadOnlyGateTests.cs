using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using NUnit.Framework;
using TwinParticles.CheckEngine.Application.Licensing;
using TwinParticles.CheckEngine.Domain.Licensing;
using TwinParticles.CheckEngine.Domain.Performance;

namespace TwinParticles.CheckEngine.Tests.Architecture;

[TestFixture]
public class LicenceReadOnlyGateTests
{
    [Test]
    public async Task GetStatusAsync_Should_Be_Inactive_When_No_Heartbeat_Recorded()
    {
        var service = CreateService(new MutableClock(DateTimeOffset.UtcNow), heartbeatUtc: null);

        var status = await service.GetStatusAsync(CancellationToken.None);

        status.IsActive.Should().BeFalse();
        status.State.Should().Be("inactive");
        status.AllowsAdminWrite.Should().BeFalse();
        status.ReasonCode.Should().Be("licence.not_activated");
    }

    [Test]
    public async Task GetStatusAsync_Should_Allow_Admin_Write_Within_Grace_Period()
    {
        var now = new DateTimeOffset(2026, 8, 14, 12, 0, 0, TimeSpan.Zero);
        var service = CreateService(new MutableClock(now), heartbeatUtc: now.AddDays(-10));

        var status = await service.GetStatusAsync(CancellationToken.None);

        status.IsActive.Should().BeTrue();
        status.State.Should().Be("active");
        status.AllowsAdminWrite.Should().BeTrue();
    }

    [Test]
    public async Task GetStatusAsync_Should_Enter_Read_Only_After_Grace_Period()
    {
        var now = new DateTimeOffset(2026, 8, 14, 12, 0, 0, TimeSpan.Zero);
        var service = CreateService(new MutableClock(now), heartbeatUtc: now.AddDays(-31));

        var status = await service.GetStatusAsync(CancellationToken.None);

        status.IsActive.Should().BeFalse();
        status.State.Should().Be("read_only");
        status.AllowsAdminWrite.Should().BeFalse();
        status.ReasonCode.Should().Be("licence.grace_expired");
    }

    [Test]
    public async Task ActivateAsync_Should_Reject_Empty_Key()
    {
        var service = CreateService(new MutableClock(DateTimeOffset.UtcNow), heartbeatUtc: null);

        var status = await service.ActivateAsync("   ", CancellationToken.None);

        status.State.Should().Be("invalid");
        status.AllowsAdminWrite.Should().BeFalse();
        status.ReasonCode.Should().Be("licence.invalid_key");
    }

    [Test]
    public async Task ActivateAsync_Should_Persist_Heartbeat_And_Return_Active()
    {
        var now = new DateTimeOffset(2026, 8, 14, 12, 0, 0, TimeSpan.Zero);
        var store = new InMemoryLicenceStateStore();
        var service = new DefaultLicenceService(store, new MutableClock(now), AcceptAllLegacyDevKeysValidator.Instance);

        var status = await service.ActivateAsync("demo-key", CancellationToken.None);

        status.IsActive.Should().BeTrue();
        status.AllowsAdminWrite.Should().BeTrue();
        (await store.GetLastHeartbeatUtcAsync(CancellationToken.None)).Should().Be(now);
    }

    [Test]
    public async Task CheckEngineLicenceGate_Should_Delegate_To_Licence_Status()
    {
        var now = new DateTimeOffset(2026, 8, 14, 12, 0, 0, TimeSpan.Zero);
        var service = CreateService(new MutableClock(now), heartbeatUtc: now.AddDays(-40));
        var gate = new CheckEngineLicenceGate(service);

        (await gate.AllowsAdminWriteAsync(CancellationToken.None)).Should().BeFalse();
    }

    private static DefaultLicenceService CreateService(ICheckEngineClock clock, DateTimeOffset? heartbeatUtc)
    {
        var store = new InMemoryLicenceStateStore();
        if (heartbeatUtc.HasValue)
        {
            store.SetLastHeartbeatUtcAsync(heartbeatUtc.Value, CancellationToken.None).GetAwaiter().GetResult();
        }

        return new DefaultLicenceService(store, clock, AcceptAllLegacyDevKeysValidator.Instance);
    }

    private sealed class AcceptAllLegacyDevKeysValidator : ILicenceKeyValidator
    {
        public static AcceptAllLegacyDevKeysValidator Instance { get; } = new();

        public LicenceKeyValidationResult Validate(string licenceKey)
            => string.IsNullOrWhiteSpace(licenceKey)
                ? new LicenceKeyValidationResult { IsValid = false, ReasonCode = "licence.invalid_key" }
                : new LicenceKeyValidationResult { IsValid = true };
    }

    private sealed class InMemoryLicenceStateStore : ILicenceStateStore
    {
        private DateTimeOffset? _heartbeatUtc;

        public Task<DateTimeOffset?> GetLastHeartbeatUtcAsync(CancellationToken cancellationToken)
            => Task.FromResult(_heartbeatUtc);

        public Task SetLastHeartbeatUtcAsync(DateTimeOffset heartbeatUtc, CancellationToken cancellationToken)
        {
            _heartbeatUtc = heartbeatUtc;
            return Task.CompletedTask;
        }
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
