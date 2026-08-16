using System;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using System.Text;
using System.Text.Json;
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
public class LicenceTierEntitlementTests
{
    private const string SigningKey = "unit-test-licence-signing-key";

    [TestCase(LicenceTier.SingleStore, false)]
    [TestCase(LicenceTier.MultiStore, false)]
    [TestCase(LicenceTier.Business, true)]
    [TestCase(LicenceTier.Enterprise, true)]
    [TestCase(LicenceTier.OemRedistribution, true)]
    [TestCase(LicenceTier.Unknown, false)]
    public void GrantsMarketplace_Should_Match_License_Matrix(LicenceTier tier, bool expected)
    {
        LicenceTierEntitlements.GrantsMarketplace(tier).Should().Be(expected);
    }

    [Test]
    public void Business_Bundle_Should_Decode_Marketplace_Entitlement()
    {
        var validator = CreateValidator();
        var bundle = CreateSignedBundle(DateTimeOffset.UtcNow.AddDays(30), tier: "Business");

        var result = validator.Validate(bundle);

        result.IsValid.Should().BeTrue();
        result.Tier.Should().Be(LicenceTier.Business);
        result.MarketplaceModuleEntitlement.Should().BeTrue();
    }

    [Test]
    public void Single_Store_Bundle_Should_Not_Grant_Marketplace()
    {
        var validator = CreateValidator();
        var bundle = CreateSignedBundle(DateTimeOffset.UtcNow.AddDays(30), tier: "Single Store");

        var result = validator.Validate(bundle);

        result.Tier.Should().Be(LicenceTier.SingleStore);
        result.MarketplaceModuleEntitlement.Should().BeFalse();
    }

    [Test]
    public void Bundle_Without_Tier_Should_Fail_Closed_On_Marketplace()
    {
        var validator = CreateValidator();
        var bundle = CreateSignedBundle(DateTimeOffset.UtcNow.AddDays(30), tier: null);

        validator.Validate(bundle).MarketplaceModuleEntitlement.Should().BeFalse();
    }

    [Test]
    public void Activate_Should_Surface_Marketplace_Entitlement_On_Status()
    {
        var store = new InMemoryLicenceStateStore();
        var now = new DateTimeOffset(2026, 8, 16, 12, 0, 0, TimeSpan.Zero);
        var service = new DefaultLicenceService(store, new FixedClock(now), CreateValidator());
        var bundle = CreateSignedBundle(now.AddDays(30), tier: "Enterprise");

        var status = service.ActivateAsync(bundle, default).GetAwaiter().GetResult();

        status.IsActive.Should().BeTrue();
        status.Tier.Should().Be(LicenceTier.Enterprise);
        status.MarketplaceModuleEntitlement.Should().BeTrue();
    }

    private static HmacLicenceKeyValidator CreateValidator()
    {
        var settings = Options.Create(new CheckEngineSettings
        {
            Licence = new CheckEngineSettings.LicenceOptions { AllowLegacyDevKeys = false }
        });

        return new HmacLicenceKeyValidator(new SecuritySettings { EncryptionKey = SigningKey }, settings);
    }

    private static string CreateSignedBundle(DateTimeOffset expiresUtc, string? tier)
    {
        object payload = tier is null
            ? new { exp = expiresUtc.ToString("O") }
            : new { exp = expiresUtc.ToString("O"), tier };
        var bytes = JsonSerializer.SerializeToUtf8Bytes(payload);
        var payloadSegment = Convert.ToBase64String(bytes);
        var signature = Convert.ToHexString(HMACSHA256.HashData(Encoding.UTF8.GetBytes(SigningKey), bytes));
        return $"{HmacLicenceKeyValidator.BundlePrefix}{payloadSegment}.{signature}";
    }

    private sealed class FixedClock : TwinParticles.CheckEngine.Domain.Performance.ICheckEngineClock
    {
        public FixedClock(DateTimeOffset utcNow) => UtcNow = utcNow;

        public DateTimeOffset UtcNow { get; }
    }

    private sealed class InMemoryLicenceStateStore : ILicenceStateStore
    {
        private DateTimeOffset? _heartbeatUtc;
        private string? _activationKey;

        public Task<DateTimeOffset?> GetLastHeartbeatUtcAsync(CancellationToken cancellationToken)
            => Task.FromResult(_heartbeatUtc);

        public Task SetLastHeartbeatUtcAsync(DateTimeOffset heartbeatUtc, CancellationToken cancellationToken)
        {
            _heartbeatUtc = heartbeatUtc;
            return Task.CompletedTask;
        }

        public Task<string?> GetActivationKeyAsync(CancellationToken cancellationToken)
            => Task.FromResult(_activationKey);

        public Task SetActivationKeyAsync(string licenceKey, CancellationToken cancellationToken)
        {
            _activationKey = licenceKey;
            return Task.CompletedTask;
        }
    }
}
