using System;
using System.Globalization;
using TwinParticles.CheckEngine.Domain.Vehicle.Aliases;

namespace TwinParticles.CheckEngine.Infrastructure.Vehicle.Aliases;

public sealed class VehicleAliasNormalizationService : IVehicleAliasNormalizationService
{
    public string Normalize(string text, string locale)
    {
        var trimmed = text.Trim();

        if (locale.StartsWith("tr", StringComparison.OrdinalIgnoreCase))
            return trimmed.ToLower(new CultureInfo("tr-TR"));

        return trimmed.ToLowerInvariant();
    }
}
