using System;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Nop.Core.Domain.Security;
using NUnit.Framework;
using TwinParticles.CheckEngine.Domain.Configuration;
using TwinParticles.CheckEngine.Infrastructure.Licensing;

namespace TwinParticles.CheckEngine.Tests.Architecture;

[TestFixture]
public class LicenceOfflineActivationTests
{
    private const string SigningKey = "unit-test-licence-signing-key";

    [Test]
    public void Signed_Bundle_Should_Activate_When_Not_Expired()
    {
        var validator = CreateValidator(allowLegacyDevKeys: false);
        var bundle = CreateSignedBundle(DateTimeOffset.UtcNow.AddDays(30));

        validator.Validate(bundle).IsValid.Should().BeTrue();
    }

    [Test]
    public void Signed_Bundle_Should_Reject_Expired_Or_Tampered_Tokens()
    {
        var validator = CreateValidator(allowLegacyDevKeys: false);
        var bundle = CreateSignedBundle(DateTimeOffset.UtcNow.AddDays(-1));
        validator.Validate(bundle).ReasonCode.Should().Be("licence.bundle_expired");

        var tampered = bundle[..^4] + "FFFF";
        validator.Validate(tampered).ReasonCode.Should().Be("licence.bundle_signature_invalid");
    }

    [Test]
    public void Legacy_Dev_Key_Should_Be_Rejected_When_Legacy_Disabled()
    {
        var validator = CreateValidator(allowLegacyDevKeys: false);
        validator.Validate("demo-key").ReasonCode.Should().Be("licence.unsigned_key_not_allowed");
    }

    [Test]
    public void AllowLegacyDevKeys_Should_Default_To_False()
    {
        new CheckEngineSettings.LicenceOptions().AllowLegacyDevKeys.Should().BeFalse();
    }

    private static HmacLicenceKeyValidator CreateValidator(bool allowLegacyDevKeys)
    {
        var settings = Options.Create(new CheckEngineSettings
        {
            Licence = new CheckEngineSettings.LicenceOptions { AllowLegacyDevKeys = allowLegacyDevKeys }
        });

        return new HmacLicenceKeyValidator(new SecuritySettings { EncryptionKey = SigningKey }, settings);
    }

    private static string CreateSignedBundle(DateTimeOffset expiresUtc)
    {
        var payload = JsonSerializer.SerializeToUtf8Bytes(new { exp = expiresUtc.ToString("O") });
        var payloadSegment = Convert.ToBase64String(payload);
        var signature = Convert.ToHexString(HMACSHA256.HashData(Encoding.UTF8.GetBytes(SigningKey), payload));
        return $"{HmacLicenceKeyValidator.BundlePrefix}{payloadSegment}.{signature}";
    }
}
