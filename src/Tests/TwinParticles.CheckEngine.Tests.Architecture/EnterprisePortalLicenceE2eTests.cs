using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using NUnit.Framework;
using TwinParticles.CheckEngine.Application.Licensing;
using TwinParticles.CheckEngine.Domain.Licensing;

namespace TwinParticles.CheckEngine.Tests.Architecture;

[TestFixture]
public class EnterprisePortalLicenceE2eTests
{
    [Test]
    public async Task Enterprise_Licence_Should_Enable_Fleet_And_Dealer_Gates()
    {
        var status = new LicenceStatus
        {
            IsActive = true,
            State = "active",
            AllowsAdminWrite = true,
            WorkshopPortalEntitlement = true,
            FleetPortalEntitlement = true,
            DealerPortalEntitlement = true
        };

        var workshop = new WorkshopPortalLicenceGate(new StubLicenceService(status));
        var fleet = new FleetPortalLicenceGate(new StubLicenceService(status));
        var dealer = new DealerPortalLicenceGate(new StubLicenceService(status));

        (await workshop.AllowsWorkshopAsync(CancellationToken.None)).Should().BeTrue();
        (await fleet.AllowsFleetAsync(CancellationToken.None)).Should().BeTrue();
        (await dealer.AllowsDealerAsync(CancellationToken.None)).Should().BeTrue();
    }

    [Test]
    public async Task Business_Licence_Should_Not_Enable_Fleet_Or_Dealer_Gates()
    {
        var status = new LicenceStatus
        {
            IsActive = true,
            State = "active",
            AllowsAdminWrite = true,
            WorkshopPortalEntitlement = true,
            FleetPortalEntitlement = false,
            DealerPortalEntitlement = false
        };

        var fleet = new FleetPortalLicenceGate(new StubLicenceService(status));
        var dealer = new DealerPortalLicenceGate(new StubLicenceService(status));

        (await fleet.AllowsFleetAsync(CancellationToken.None)).Should().BeFalse();
        (await dealer.AllowsDealerAsync(CancellationToken.None)).Should().BeFalse();
    }

    private sealed class StubLicenceService : ILicenceService
    {
        private readonly LicenceStatus _status;

        public StubLicenceService(LicenceStatus status) => _status = status;

        public Task<LicenceStatus> GetStatusAsync(CancellationToken cancellationToken) => Task.FromResult(_status);

        public Task<LicenceStatus> ActivateAsync(string licenceKey, CancellationToken cancellationToken) => GetStatusAsync(cancellationToken);

        public Task<LicenceStatus> HeartbeatAsync(CancellationToken cancellationToken) => GetStatusAsync(cancellationToken);
    }
}
