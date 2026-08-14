using System.Security.Cryptography;
using System.Text;
using Nop.Core.Domain.Security;
using TwinParticles.CheckEngine.Domain.Search;

namespace TwinParticles.CheckEngine.Infrastructure.Search;

/// <summary>
/// Keyed fingerprints prevent dictionary reversal of short VIN/OEM/query strings if analytics data
/// is exposed. The host encryption key supplies the deployment-specific HMAC secret.
/// </summary>
public sealed class HmacSearchQueryFingerprintService : ISearchQueryFingerprintService
{
    private readonly byte[] _key;

    public HmacSearchQueryFingerprintService(SecuritySettings securitySettings)
    {
        _key = Encoding.UTF8.GetBytes(securitySettings.EncryptionKey);
    }

    public string Create(string normalizedQuery)
    {
        var bytes = Encoding.UTF8.GetBytes(normalizedQuery ?? string.Empty);
        return Convert.ToHexString(HMACSHA256.HashData(_key, bytes));
    }
}
