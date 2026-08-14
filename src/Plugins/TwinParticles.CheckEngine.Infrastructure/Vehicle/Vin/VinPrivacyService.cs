using System;
using System.Security.Cryptography;
using System.Text;
using Nop.Core.Domain.Security;
using TwinParticles.CheckEngine.Domain.Vehicle;

namespace TwinParticles.CheckEngine.Infrastructure.Vehicle.Vin;

public sealed class VinPrivacyService : IVinPrivacyService
{
    private readonly byte[] _key;

    public VinPrivacyService(SecuritySettings securitySettings)
    {
        _key = Encoding.UTF8.GetBytes(securitySettings.EncryptionKey ?? string.Empty);
    }

    public string? GetLast4(string? normalizedVin)
    {
        if (string.IsNullOrWhiteSpace(normalizedVin) || normalizedVin.Length < 4)
            return null;

        return normalizedVin[^4..];
    }

    public string CreateHash(string normalizedVin)
    {
        var bytes = Encoding.UTF8.GetBytes(normalizedVin ?? string.Empty);
        return Convert.ToHexString(HMACSHA256.HashData(_key, bytes));
    }
}
