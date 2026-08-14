using System;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using TwinParticles.CheckEngine.Domain.Configuration;
using TwinParticles.CheckEngine.Domain.Erp;

namespace TwinParticles.CheckEngine.Infrastructure.Erp;

public sealed class HmacErpInboundWebhookValidator : IErpInboundWebhookValidator
{
    public const string SignatureHeaderName = "X-CheckEngine-Erp-Signature";

    private readonly CheckEngineSettings _settings;

    public HmacErpInboundWebhookValidator(IOptions<CheckEngineSettings> settings)
    {
        _settings = settings.Value;
    }

    public ErpInboundWebhookValidationResult Validate(string rawBody, string? signatureHeader)
    {
        var secret = _settings.Erp.InboundWebhookSecret?.Trim();
        if (string.IsNullOrWhiteSpace(secret))
            return Invalid("erp.webhook.secret_not_configured");

        if (string.IsNullOrWhiteSpace(signatureHeader))
            return Invalid("erp.webhook.missing_signature");

        var body = rawBody ?? string.Empty;
        var expected = Convert.ToHexString(HMACSHA256.HashData(Encoding.UTF8.GetBytes(secret), Encoding.UTF8.GetBytes(body)));
        var provided = signatureHeader.Trim();
        if (provided.StartsWith("sha256=", StringComparison.OrdinalIgnoreCase))
            provided = provided["sha256=".Length..];

        if (!CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(expected),
                Encoding.UTF8.GetBytes(provided.ToUpperInvariant())))
            return Invalid("erp.webhook.invalid_signature");

        return new ErpInboundWebhookValidationResult { IsValid = true };
    }

    private static ErpInboundWebhookValidationResult Invalid(string reasonCode) => new()
    {
        IsValid = false,
        ReasonCode = reasonCode
    };
}
