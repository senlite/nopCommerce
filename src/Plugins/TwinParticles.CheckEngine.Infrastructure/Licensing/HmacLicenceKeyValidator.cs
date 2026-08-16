using System;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using Nop.Core.Domain.Security;
using TwinParticles.CheckEngine.Domain.Configuration;
using TwinParticles.CheckEngine.Domain.Licensing;

namespace TwinParticles.CheckEngine.Infrastructure.Licensing;

public sealed class HmacLicenceKeyValidator : ILicenceKeyValidator
{
    public const string BundlePrefix = "ce-lic-v1.";

    private readonly byte[] _signingKey;
    private readonly CheckEngineSettings _settings;

    public HmacLicenceKeyValidator(SecuritySettings securitySettings, IOptions<CheckEngineSettings> settings)
    {
        _signingKey = Encoding.UTF8.GetBytes(securitySettings.EncryptionKey ?? string.Empty);
        _settings = settings.Value;
    }

    public LicenceKeyValidationResult Validate(string licenceKey)
    {
        if (string.IsNullOrWhiteSpace(licenceKey))
            return Invalid("licence.invalid_key");

        var trimmed = licenceKey.Trim();
        if (!trimmed.StartsWith(BundlePrefix, StringComparison.Ordinal))
        {
            return _settings.Licence.AllowLegacyDevKeys
                ? Valid(expiresUtc: null)
                : Invalid("licence.unsigned_key_not_allowed");
        }

        var token = trimmed[BundlePrefix.Length..];
        var dot = token.LastIndexOf('.');
        if (dot <= 0 || dot >= token.Length - 1)
            return Invalid("licence.bundle_malformed");

        var payloadSegment = token[..dot];
        var signatureSegment = token[(dot + 1)..];

        byte[] payloadBytes;
        try
        {
            payloadBytes = Convert.FromBase64String(payloadSegment);
        }
        catch
        {
            return Invalid("licence.bundle_malformed");
        }

        var expected = Convert.ToHexString(HMACSHA256.HashData(_signingKey, payloadBytes));
        if (!CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(expected),
                Encoding.UTF8.GetBytes(signatureSegment.ToUpperInvariant())))
            return Invalid("licence.bundle_signature_invalid");

        try
        {
            using var document = JsonDocument.Parse(payloadBytes);
            if (!document.RootElement.TryGetProperty("exp", out var expElement))
                return Invalid("licence.bundle_missing_expiry");

            var expText = expElement.GetString();
            if (!DateTimeOffset.TryParse(expText, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var expiresUtc))
                return Invalid("licence.bundle_invalid_expiry");

            if (expiresUtc <= DateTimeOffset.UtcNow)
                return Invalid("licence.bundle_expired");

            var tier = LicenceTier.Unknown;
            if (document.RootElement.TryGetProperty("tier", out var tierElement))
                tier = LicenceTierEntitlements.ParseTier(tierElement.GetString());

            var marketplace = LicenceTierEntitlements.GrantsMarketplace(tier);
            if (TryReadMarketplaceFlag(document.RootElement, out var explicitMarketplace))
                marketplace = explicitMarketplace;

            return Valid(expiresUtc, tier, marketplace);
        }
        catch
        {
            return Invalid("licence.bundle_malformed");
        }
    }

    private static bool TryReadMarketplaceFlag(JsonElement root, out bool marketplace)
    {
        foreach (var name in new[] { "marketplace", "MarketplaceModuleEntitlement" })
        {
            if (!root.TryGetProperty(name, out var element))
                continue;

            if (element.ValueKind == JsonValueKind.True)
            {
                marketplace = true;
                return true;
            }

            if (element.ValueKind == JsonValueKind.False)
            {
                marketplace = false;
                return true;
            }

            if (element.ValueKind == JsonValueKind.String && bool.TryParse(element.GetString(), out var parsed))
            {
                marketplace = parsed;
                return true;
            }
        }

        marketplace = false;
        return false;
    }

    private static LicenceKeyValidationResult Valid(DateTimeOffset? expiresUtc, LicenceTier tier = LicenceTier.Unknown, bool marketplace = false) => new()
    {
        IsValid = true,
        ExpiresUtc = expiresUtc,
        Tier = tier,
        MarketplaceModuleEntitlement = marketplace
    };

    private static LicenceKeyValidationResult Invalid(string reasonCode) => new()
    {
        IsValid = false,
        ReasonCode = reasonCode
    };
}
