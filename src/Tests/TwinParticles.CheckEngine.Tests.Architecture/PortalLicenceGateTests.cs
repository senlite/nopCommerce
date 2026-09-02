using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Nop.Core.Domain.Security;
using NUnit.Framework;
using TwinParticles.CheckEngine.Application.Licensing;
using TwinParticles.CheckEngine.Domain.Configuration;
using TwinParticles.CheckEngine.Domain.Licensing;
using TwinParticles.CheckEngine.Infrastructure.Licensing;

namespace TwinParticles.CheckEngine.Tests.Architecture;

[TestFixture]
public class PortalLicenceGateTests
{
    private const string SigningKey = "unit-test-licence-signing-key";

    [Test]
    public async Task Workshop_Gate_Should_Require_WorkshopPortalEntitlement()
    {
        var denied = new WorkshopPortalLicenceGate(new StubLicenceService(workshop: false));
        var allowed = new WorkshopPortalLicenceGate(new StubLicenceService(workshop: true));

        (await denied.AllowsWorkshopAsync(CancellationToken.None)).Should().BeFalse();
        (await allowed.AllowsWorkshopAsync(CancellationToken.None)).Should().BeTrue();
    }

    [Test]
    public async Task Fleet_Gate_Should_Require_FleetPortalEntitlement()
    {
        var denied = new FleetPortalLicenceGate(new StubLicenceService(fleet: false));
        var allowed = new FleetPortalLicenceGate(new StubLicenceService(fleet: true));

        (await denied.AllowsFleetAsync(CancellationToken.None)).Should().BeFalse();
        (await allowed.AllowsFleetAsync(CancellationToken.None)).Should().BeTrue();
    }

    [Test]
    public async Task Dealer_Gate_Should_Require_DealerPortalEntitlement()
    {
        var denied = new DealerPortalLicenceGate(new StubLicenceService(dealer: false));
        var allowed = new DealerPortalLicenceGate(new StubLicenceService(dealer: true));

        (await denied.AllowsDealerAsync(CancellationToken.None)).Should().BeFalse();
        (await allowed.AllowsDealerAsync(CancellationToken.None)).Should().BeTrue();
    }

    [Test]
    public void Enterprise_Bundle_With_Portal_Flags_Should_Decode_All_Entitlements()
    {
        var validator = CreateValidator();
        var bundle = CreateSignedBundle(
            DateTimeOffset.UtcNow.AddDays(30),
            tier: "Enterprise",
            workshop: true,
            fleet: true,
            dealer: true);

        var result = validator.Validate(bundle);

        result.IsValid.Should().BeTrue();
        result.WorkshopPortalEntitlement.Should().BeTrue();
        result.FleetPortalEntitlement.Should().BeTrue();
        result.DealerPortalEntitlement.Should().BeTrue();
    }

    [TestCase(LicenceTier.Business, true, false, false)]
    [TestCase(LicenceTier.Enterprise, true, true, true)]
    public void Portal_Tier_Matrix_Should_Match_Docs(LicenceTier tier, bool workshop, bool fleet, bool dealer)
    {
        LicenceTierEntitlements.GrantsWorkshopPortal(tier).Should().Be(workshop);
        LicenceTierEntitlements.GrantsFleetPortal(tier).Should().Be(fleet);
        LicenceTierEntitlements.GrantsDealerPortal(tier).Should().Be(dealer);
    }

    private static HmacLicenceKeyValidator CreateValidator()
    {
        var settings = Options.Create(new CheckEngineSettings
        {
            Licence = new CheckEngineSettings.LicenceOptions { AllowLegacyDevKeys = false }
        });

        return new HmacLicenceKeyValidator(new SecuritySettings { EncryptionKey = SigningKey }, settings);
    }

    private static string CreateSignedBundle(
        DateTimeOffset expiresUtc,
        string tier,
        bool workshop,
        bool fleet,
        bool dealer)
    {
        var payload = new Dictionary<string, object>
        {
            ["exp"] = expiresUtc.ToString("O"),
            ["tier"] = tier,
            ["workshop"] = workshop,
            ["fleet"] = fleet,
            ["dealer"] = dealer
        };
        var bytes = JsonSerializer.SerializeToUtf8Bytes(payload);
        var payloadSegment = Convert.ToBase64String(bytes);
        var signature = Convert.ToHexString(HMACSHA256.HashData(Encoding.UTF8.GetBytes(SigningKey), bytes));
        return $"{HmacLicenceKeyValidator.BundlePrefix}{payloadSegment}.{signature}";
    }

    private sealed class StubLicenceService : ILicenceService
    {
        private readonly LicenceStatus _status;

        public StubLicenceService(bool workshop = false, bool fleet = false, bool dealer = false)
        {
            _status = new LicenceStatus
            {
                IsActive = true,
                State = "active",
                AllowsAdminWrite = true,
                Tier = LicenceTier.Enterprise,
                WorkshopPortalEntitlement = workshop,
                FleetPortalEntitlement = fleet,
                DealerPortalEntitlement = dealer
            };
        }

        public Task<LicenceStatus> GetStatusAsync(CancellationToken cancellationToken) => Task.FromResult(_status);

        public Task<LicenceStatus> ActivateAsync(string licenceKey, CancellationToken cancellationToken)
            => GetStatusAsync(cancellationToken);

        public Task<LicenceStatus> HeartbeatAsync(CancellationToken cancellationToken)
            => GetStatusAsync(cancellationToken);
    }
}
