using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;

namespace Nop.Plugin.Payments.Paymob.Services;

/// <summary>
/// Flattens Paymob callback query/form/json into the dotted field names HMAC verification expects.
/// </summary>
public static class PaymobCallbackFieldReader
{
    public static IReadOnlyDictionary<string, string?> FromQuery(IQueryCollection query)
        => FromPairs(query.Select(pair => (pair.Key, ValuesToString(pair.Value))));

    public static IReadOnlyDictionary<string, string?> FromForm(IFormCollection form)
        => FromPairs(form.Select(pair => (pair.Key, ValuesToString(pair.Value))));

    public static IReadOnlyDictionary<string, string?> FromPairs(IEnumerable<(string Key, string? Value)> pairs)
    {
        var fields = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        foreach (var (key, value) in pairs)
        {
            if (string.IsNullOrWhiteSpace(key))
                continue;

            var normalized = key.StartsWith("obj.", StringComparison.OrdinalIgnoreCase)
                ? key["obj.".Length..]
                : key;
            fields[normalized] = value;
        }

        return fields;
    }

    public static string? ReadHmac(IReadOnlyDictionary<string, string?> fields, string? queryHmac)
    {
        if (!string.IsNullOrWhiteSpace(queryHmac))
            return queryHmac;

        return fields.TryGetValue("hmac", out var hmac) ? hmac : null;
    }

    public static bool IsSuccessful(IReadOnlyDictionary<string, string?> fields)
        => fields.TryGetValue("success", out var success) &&
           string.Equals(success, "true", StringComparison.OrdinalIgnoreCase);

    public static Guid? TryReadOrderGuid(IReadOnlyDictionary<string, string?> fields)
    {
        foreach (var key in new[] { "merchant_order_id", "order.merchant_order_id", "order.id" })
        {
            if (fields.TryGetValue(key, out var raw) && Guid.TryParse(raw, out var guid))
                return guid;
        }

        return null;
    }

    public static int? TryReadOrderId(IReadOnlyDictionary<string, string?> fields)
    {
        foreach (var key in new[] { "merchant_order_id", "order.merchant_order_id" })
        {
            if (fields.TryGetValue(key, out var raw) &&
                int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var id))
                return id;
        }

        return null;
    }

    private static string? ValuesToString(StringValues values)
        => values.Count == 0 ? null : values.ToString();
}
