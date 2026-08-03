using System;
using System.Globalization;
using TwinParticles.CheckEngine.Domain.L10n;

namespace TwinParticles.CheckEngine.Infrastructure.L10n;

public sealed class DefaultLocaleFormattingService : ILocaleFormattingService
{
    public string FormatNumber(decimal value, string locale)
    {
        var culture = ResolveCulture(locale);
        return value.ToString("N2", culture);
    }

    public string FormatDate(DateTime value, string locale)
    {
        var culture = ResolveCulture(locale);
        return value.ToString("d", culture);
    }

    public string FormatUnit(decimal value, string unitCode, string locale)
    {
        var culture = ResolveCulture(locale);
        var formatted = value.ToString("N0", culture);
        var unit = string.IsNullOrWhiteSpace(unitCode) ? "unit" : unitCode.Trim();
        return $"{formatted} {unit}";
    }

    private static CultureInfo ResolveCulture(string locale)
    {
        if (locale.StartsWith("ar", StringComparison.OrdinalIgnoreCase))
            return new CultureInfo("ar");

        return CultureInfo.InvariantCulture;
    }
}
