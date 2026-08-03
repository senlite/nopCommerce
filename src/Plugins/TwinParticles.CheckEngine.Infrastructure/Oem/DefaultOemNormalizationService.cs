using System;
using System.Text;
using TwinParticles.CheckEngine.Domain.Oem;

namespace TwinParticles.CheckEngine.Infrastructure.Oem;

public sealed class DefaultOemNormalizationService : IOemNormalizationService
{
    public string Normalize(string rawNumber)
    {
        if (rawNumber is null)
            return string.Empty;

        var trimmed = rawNumber.Trim();
        var buffer = new StringBuilder(trimmed.Length);

        foreach (var character in trimmed)
        {
            if (char.IsLetterOrDigit(character))
            {
                buffer.Append(char.ToUpperInvariant(character));
            }
        }

        return buffer.ToString();
    }
}
