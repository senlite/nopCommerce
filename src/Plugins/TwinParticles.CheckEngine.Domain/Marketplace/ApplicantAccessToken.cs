using System;
using System.Security.Cryptography;
using System.Text;

namespace TwinParticles.CheckEngine.Domain.Marketplace;

/// <summary>
/// Apply-time guest access token. The plaintext is returned once on apply; only the SHA-256 hash
/// is stored. Status and AcceptAgreement accept the plaintext for guests who have no customer id.
/// </summary>
public static class ApplicantAccessToken
{
    public static string NewPlaintext() => Guid.NewGuid().ToString("N");

    public static string Hash(string plaintext)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(plaintext);
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(plaintext.Trim())));
    }

    public static bool Matches(string? storedHash, string? plaintext)
    {
        if (string.IsNullOrWhiteSpace(storedHash) || string.IsNullOrWhiteSpace(plaintext))
            return false;

        var storedBytes = DecodeHex(storedHash);
        var providedBytes = DecodeHex(Hash(plaintext));
        if (storedBytes.Length == 0 || storedBytes.Length != providedBytes.Length)
            return false;

        return CryptographicOperations.FixedTimeEquals(storedBytes, providedBytes);
    }

    private static byte[] DecodeHex(string value)
    {
        try
        {
            return Convert.FromHexString(value.Trim());
        }
        catch (FormatException)
        {
            return [];
        }
    }
}
