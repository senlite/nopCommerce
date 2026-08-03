using System.Globalization;
using TwinParticles.CheckEngine.Domain.Search;

namespace TwinParticles.CheckEngine.Infrastructure.Search;

public sealed class DefaultBilingualSearchTextNormalizer : IBilingualSearchTextNormalizer
{
    public string Normalize(string text, string locale)
    {
        var value = text?.Trim() ?? string.Empty;

        if (locale.StartsWith("ar"))
            return value.ToLower(new CultureInfo("ar"));

        return value.ToLowerInvariant();
    }
}
