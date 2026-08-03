using System;

namespace TwinParticles.CheckEngine.Domain.Vehicle;

public sealed class Vin : IEquatable<Vin>
{
    private const int VinLength = 17;
    private static readonly int[] PositionWeights = [8, 7, 6, 5, 4, 3, 2, 10, 0, 9, 8, 7, 6, 5, 4, 3, 2];

    private Vin(string value)
    {
        Value = value;
    }

    public string Value { get; }

    public static Vin Create(string raw, bool enforceCheckDigit = true)
    {
        if (TryCreate(raw, out var vin, out var errorCode, enforceCheckDigit))
        {
            return vin!;
        }

        throw new ArgumentException(errorCode, nameof(raw));
    }

    public static bool TryCreate(string raw, out Vin? vin, out string? errorCode, bool enforceCheckDigit = true)
    {
        vin = null;
        errorCode = null;

        if (raw is null)
        {
            errorCode = "vin.invalid_length";
            return false;
        }

        var normalized = Normalize(raw);
        if (normalized.Length != VinLength)
        {
            errorCode = "vin.invalid_length";
            return false;
        }

        if (!HasValidCharset(normalized))
        {
            errorCode = "vin.invalid_charset";
            return false;
        }

        if (enforceCheckDigit && !IsCheckDigitValid(normalized))
        {
            errorCode = "vin.check_digit_failed";
            return false;
        }

        vin = new Vin(normalized);
        return true;
    }

    public static bool IsCheckDigitValid(string normalizedVin)
    {
        if (normalizedVin is null || normalizedVin.Length != VinLength)
        {
            return false;
        }

        if (!HasValidCharset(normalizedVin))
        {
            return false;
        }

        var expected = ComputeCheckDigit(normalizedVin);
        return normalizedVin[8] == expected;
    }

    public VinSegments ParseSegments()
    {
        return VinSegments.FromVin(this);
    }

    public override string ToString()
    {
        return Value;
    }

    public bool Equals(Vin? other)
    {
        return other is not null && string.Equals(Value, other.Value, StringComparison.Ordinal);
    }

    public override bool Equals(object? obj)
    {
        return obj is Vin other && Equals(other);
    }

    public override int GetHashCode()
    {
        return StringComparer.Ordinal.GetHashCode(Value);
    }

    private static string Normalize(string raw)
    {
        var trimmed = raw.Trim();
        var buffer = new char[trimmed.Length];
        var index = 0;

        foreach (var character in trimmed)
        {
            if (character == ' ' || character == '-')
            {
                continue;
            }

            buffer[index] = char.ToUpperInvariant(character);
            index++;
        }

        return new string(buffer, 0, index);
    }

    private static bool HasValidCharset(string normalizedVin)
    {
        foreach (var character in normalizedVin)
        {
            if (Transliterate(character) < 0)
            {
                return false;
            }
        }

        return true;
    }

    private static char ComputeCheckDigit(string normalizedVin)
    {
        var sum = 0;
        for (var index = 0; index < normalizedVin.Length; index++)
        {
            var value = Transliterate(normalizedVin[index]);
            sum += value * PositionWeights[index];
        }

        var remainder = sum % 11;
        return remainder == 10 ? 'X' : (char)('0' + remainder);
    }

    private static int Transliterate(char character)
    {
        return character switch
        {
            >= '0' and <= '9' => character - '0',
            'A' or 'J' => 1,
            'B' or 'K' or 'S' => 2,
            'C' or 'L' or 'T' => 3,
            'D' or 'M' or 'U' => 4,
            'E' or 'N' or 'V' => 5,
            'F' or 'W' => 6,
            'G' or 'P' or 'X' => 7,
            'H' or 'Y' => 8,
            'R' or 'Z' => 9,
            _ => -1
        };
    }
}
