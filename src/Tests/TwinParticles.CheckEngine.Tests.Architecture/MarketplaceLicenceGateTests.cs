using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using NUnit.Framework;
using TwinParticles.CheckEngine.Application.Licensing;
using TwinParticles.CheckEngine.Domain.Licensing;

namespace TwinParticles.CheckEngine.Tests.Architecture;

[TestFixture]
public class MarketplaceLicenceGateTests
{
    [Test]
    public async Task AllowsMarketplace_Should_Require_Entitlement_Not_Just_An_Active_Licence()
    {
        var denied = new MarketplaceLicenceGate(new StubLicenceService(entitled: false));
        var allowed = new MarketplaceLicenceGate(new StubLicenceService(entitled: true));

        (await denied.AllowsMarketplaceAsync(CancellationToken.None)).Should().BeFalse();
        (await allowed.AllowsMarketplaceAsync(CancellationToken.None)).Should().BeTrue();
    }

    [Test]
    public async Task Single_Store_Licence_Should_Deny_Marketplace_Even_When_Active()
    {
        var gate = new MarketplaceLicenceGate(new StubLicenceService(
            entitled: false,
            tier: LicenceTier.SingleStore,
            isActive: true));

        (await gate.AllowsMarketplaceAsync(CancellationToken.None)).Should().BeFalse();
    }

    private sealed class StubLicenceService : ILicenceService
    {
        private readonly LicenceStatus _status;

        public StubLicenceService(bool entitled, LicenceTier tier = LicenceTier.Business, bool isActive = true)
        {
            _status = new LicenceStatus
            {
                IsActive = isActive,
                State = isActive ? "active" : "inactive",
                AllowsAdminWrite = isActive,
                Tier = tier,
                MarketplaceModuleEntitlement = entitled
            };
        }

        public Task<LicenceStatus> GetStatusAsync(CancellationToken cancellationToken) => Task.FromResult(_status);

        public Task<LicenceStatus> ActivateAsync(string licenceKey, CancellationToken cancellationToken)
            => GetStatusAsync(cancellationToken);

        public Task<LicenceStatus> HeartbeatAsync(CancellationToken cancellationToken)
            => GetStatusAsync(cancellationToken);
    }
}
