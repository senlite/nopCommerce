using System;
using System.Collections.Generic;
using System.Linq;

namespace TwinParticles.CheckEngine.Infrastructure.Vehicle.Vin;

public sealed class BmwVinWmiAllowList
{
    public BmwVinWmiAllowList(IEnumerable<string> wmis)
    {
        SupportedWmis = wmis
            .Where(wmi => !string.IsNullOrWhiteSpace(wmi))
            .Select(wmi => wmi.Trim().ToUpperInvariant())
            .Distinct(StringComparer.Ordinal)
            .ToArray();
    }

    public IReadOnlyList<string> SupportedWmis { get; }

    public bool Contains(string wmi)
    {
        if (string.IsNullOrWhiteSpace(wmi))
            return false;

        return SupportedWmis.Contains(wmi.Trim().ToUpperInvariant(), StringComparer.Ordinal);
    }
}
