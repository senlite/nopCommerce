using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace Nop.Plugin.Payments.Paymob.Services;

/// <summary>
/// Verifies Paymob transaction callbacks. Paymob concatenates a fixed, documented ordered set of
/// transaction fields and signs them with HMAC-SHA512 using the merchant HMAC secret. A callback is
/// authentic only when the recomputed signature matches the supplied one; verification is
/// constant-time to avoid timing oracles.
/// </summary>
public sealed class PaymobHmacValidator
{
    /// <summary>The exact field order Paymob uses to build the HMAC payload.</summary>
    public static readonly IReadOnlyList<string> SignedFieldOrder =
    [
        "amount_cents", "created_at", "currency", "error_occured", "has_parent_transaction",
        "id", "integration_id", "is_3d_secure", "is_auth", "is_capture", "is_refunded",
        "is_standalone_payment", "is_voided", "order.id", "owner", "pending",
        "source_data.pan", "source_data.sub_type", "source_data.type", "success"
    ];

    /// <summary>Computes the lowercase hex HMAC-SHA512 over the fields in the documented order.</summary>
    public string Compute(IReadOnlyDictionary<string, string?> fields, string hmacSecret)
    {
        ArgumentNullException.ThrowIfNull(fields);
        if (string.IsNullOrEmpty(hmacSecret))
            throw new ArgumentException("Paymob HMAC secret is required.", nameof(hmacSecret));

        var concatenated = new StringBuilder();
        foreach (var key in SignedFieldOrder)
            concatenated.Append(fields.TryGetValue(key, out var value) ? value ?? string.Empty : string.Empty);

        using var hmac = new HMACSHA512(Encoding.UTF8.GetBytes(hmacSecret));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(concatenated.ToString()));
        return Convert.ToHexStringLower(hash);
    }

    /// <summary>True when the recomputed signature matches <paramref name="providedHmac"/>.</summary>
    public bool Verify(IReadOnlyDictionary<string, string?> fields, string hmacSecret, string? providedHmac)
    {
        if (string.IsNullOrWhiteSpace(providedHmac))
            return false;

        var expected = Compute(fields, hmacSecret);
        // Case-insensitive, fixed-length, constant-time comparison.
        if (expected.Length != providedHmac.Length)
            return false;

        return CryptographicOperations.FixedTimeEquals(
            Encoding.ASCII.GetBytes(expected),
            Encoding.ASCII.GetBytes(providedHmac.ToLowerInvariant()));
    }
}
