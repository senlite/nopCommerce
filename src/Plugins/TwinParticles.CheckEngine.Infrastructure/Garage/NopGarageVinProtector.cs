using Nop.Services.Security;
using TwinParticles.CheckEngine.Domain.Garage;

namespace TwinParticles.CheckEngine.Infrastructure.Garage;

/// <summary>
/// Uses nopCommerce's configured encryption key to protect garage VINs at rest (FR-716). The prefix
/// supports safe mixed-version reads: legacy plaintext remains readable and is encrypted on the next
/// garage save, while encrypted values are never mistaken for VIN input.
/// </summary>
public sealed class NopGarageVinProtector : IGarageVinProtector
{
    private const string Prefix = "enc:v1:";

    private readonly IEncryptionService _encryptionService;

    public NopGarageVinProtector(IEncryptionService encryptionService)
    {
        _encryptionService = encryptionService;
    }

    public string? Protect(string? vin)
    {
        if (string.IsNullOrWhiteSpace(vin))
            return null;

        if (vin.StartsWith(Prefix, System.StringComparison.Ordinal))
            return vin;

        return Prefix + _encryptionService.EncryptText(vin.Trim().ToUpperInvariant());
    }

    public string? Unprotect(string? storedValue)
    {
        if (string.IsNullOrWhiteSpace(storedValue))
            return null;

        if (!storedValue.StartsWith(Prefix, System.StringComparison.Ordinal))
            return storedValue.Trim().ToUpperInvariant();

        return _encryptionService.DecryptText(storedValue[Prefix.Length..]);
    }
}
