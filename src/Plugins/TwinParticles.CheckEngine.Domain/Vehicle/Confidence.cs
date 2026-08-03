using System;

namespace TwinParticles.CheckEngine.Domain.Vehicle;

public sealed class Confidence : IEquatable<Confidence>
{
    private Confidence(decimal value)
    {
        Value = value;
    }

    public decimal Value { get; }

    public static Confidence Create(decimal value)
    {
        if (TryCreate(value, out var confidence, out var errorCode))
        {
            return confidence!;
        }

        throw new ArgumentOutOfRangeException(nameof(value), errorCode);
    }

    public static bool TryCreate(decimal value, out Confidence? confidence, out string? errorCode)
    {
        confidence = null;
        errorCode = null;

        if (value < 0m || value > 1m)
        {
            errorCode = "vin.confidence_out_of_range";
            return false;
        }

        confidence = new Confidence(value);
        return true;
    }

    public bool Equals(Confidence? other)
    {
        return other is not null && Value == other.Value;
    }

    public override bool Equals(object? obj)
    {
        return obj is Confidence other && Equals(other);
    }

    public override int GetHashCode()
    {
        return Value.GetHashCode();
    }

    public override string ToString()
    {
        return Value.ToString("0.00");
    }
}
