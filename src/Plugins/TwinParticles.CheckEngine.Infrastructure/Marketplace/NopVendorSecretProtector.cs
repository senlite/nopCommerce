using System;
using Nop.Services.Security;
using TwinParticles.CheckEngine.Domain.Marketplace;

namespace TwinParticles.CheckEngine.Infrastructure.Marketplace;

/// <summary>
/// Encrypts vendor banking details at rest. Ciphertext never leaves the repository toward APIs.
/// </summary>
public sealed class NopVendorSecretProtector : IVendorSecretProtector
{
    public const string Prefix = "enc:v1:";

    private readonly IEncryptionService _encryptionService;

    public NopVendorSecretProtector(IEncryptionService encryptionService)
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
}
