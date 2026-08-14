using System;
using System.Collections.Generic;

namespace Nop.Plugin.Shipping.Bosta.Services;

/// <summary>
/// Pure, deterministic Bosta domestic rate model: a base fee per destination zone plus a per-kilogram
/// fee for weight beyond the first kilogram. Kept free of nopCommerce types so it is unit-testable.
/// </summary>
public sealed class BostaRateCalculator
{
    // Bosta's documented Egyptian zone tiers. Unknown cities fall back to the national "Other" zone.
    private static readonly Dictionary<string, decimal> ZoneBaseFees = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Cairo"] = 45m,
        ["Giza"] = 45m,
        ["Alexandria"] = 55m,
        ["Delta"] = 60m,
        ["Canal"] = 60m,
        ["Upper Egypt"] = 70m,
        ["Other"] = 80m
    };

    public const decimal PerAdditionalKgFee = 10m;
    public const decimal FirstKgIncluded = 1m;

    /// <summary>Resolves the base fee for a destination zone, falling back to the national rate.</summary>
    public decimal ResolveZoneBaseFee(string? zone)
    {
        if (!string.IsNullOrWhiteSpace(zone) && ZoneBaseFees.TryGetValue(zone.Trim(), out var fee))
            return fee;

        return ZoneBaseFees["Other"];
    }

    /// <summary>
    /// Computes the domestic shipping rate. Weight is billed in whole kilograms (rounded up); the
    /// first kilogram is included in the zone base fee.
    /// </summary>
    public decimal CalculateRate(string? zone, decimal totalWeightKg)
    {
        var baseFee = ResolveZoneBaseFee(zone);
        if (totalWeightKg <= FirstKgIncluded)
            return baseFee;

        var billableKg = Math.Ceiling(totalWeightKg - FirstKgIncluded);
        return baseFee + (billableKg * PerAdditionalKgFee);
    }
}
