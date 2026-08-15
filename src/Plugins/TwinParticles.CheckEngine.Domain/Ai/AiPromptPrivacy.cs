using System.Text.RegularExpressions;

namespace TwinParticles.CheckEngine.Domain.Ai;

/// <summary>
/// Strips full VINs from outbound AI prompts. Logged and transmitted text may keep last-4 only.
/// </summary>
public static class AiPromptPrivacy
{
    private static readonly Regex VinPattern = new(
        @"\b[A-HJ-NPR-Z0-9]{17}\b",
        RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    public static string RedactVins(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return text ?? string.Empty;

        return VinPattern.Replace(text, match =>
        {
            var vin = match.Value;
            return vin.Length >= 4 ? $"VIN …{vin[^4..]}" : "VIN …****";
        });
    }

    public static bool ContainsFullVin(string text)
    {
        return !string.IsNullOrWhiteSpace(text) && VinPattern.IsMatch(text);
    }
}
