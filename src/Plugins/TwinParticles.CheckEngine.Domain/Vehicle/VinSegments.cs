using System;

namespace TwinParticles.CheckEngine.Domain.Vehicle;

public sealed class VinSegments : IEquatable<VinSegments>
{
    private VinSegments(string wmi, string vds, string vis)
    {
        Wmi = wmi;
        Vds = vds;
        Vis = vis;
    }

    public string Wmi { get; }

    public string Vds { get; }

    public string Vis { get; }

    public static VinSegments FromVin(Vin vin)
    {
        ArgumentNullException.ThrowIfNull(vin);

        var value = vin.Value;
        return new VinSegments(
            value[..3],
            value.Substring(3, 6),
            value.Substring(9, 8));
    }

    public bool Equals(VinSegments? other)
    {
        return other is not null
               && string.Equals(Wmi, other.Wmi, StringComparison.Ordinal)
               && string.Equals(Vds, other.Vds, StringComparison.Ordinal)
               && string.Equals(Vis, other.Vis, StringComparison.Ordinal);
    }

    public override bool Equals(object? obj)
    {
        return obj is VinSegments other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(
            StringComparer.Ordinal.GetHashCode(Wmi),
            StringComparer.Ordinal.GetHashCode(Vds),
            StringComparer.Ordinal.GetHashCode(Vis));
    }
}
