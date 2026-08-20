using System;
using Nop.Services.Security;
using TwinParticles.CheckEngine.Domain.Tenancy;

namespace TwinParticles.CheckEngine.Infrastructure.Tenancy;

public sealed class NopTenantWebhookSecretProtector : ITenantWebhookSecretProtector
{
    public const string Prefix = "enc:v1:";

    private readonly IEncryptionService _encryptionService;

    public NopTenantWebhookSecretProtector(IEncryptionService encryptionService)
    {
        _encryptionService = encryptionService;
    }

    public string? Protect(string? plaintext)
    {
        if (string.IsNullOrWhiteSpace(plaintext))
            return null;

        if (plaintext.StartsWith(Prefix, StringComparison.Ordinal))
            return plaintext;

        return Prefix + _encryptionService.EncryptText(plaintext.Trim());
    }

    public string? Unprotect(string? storedValue)
    {
        if (string.IsNullOrWhiteSpace(storedValue))
            return null;

        if (!storedValue.StartsWith(Prefix, StringComparison.Ordinal))
            return storedValue;

        return _encryptionService.DecryptText(storedValue[Prefix.Length..]);
    }
}
