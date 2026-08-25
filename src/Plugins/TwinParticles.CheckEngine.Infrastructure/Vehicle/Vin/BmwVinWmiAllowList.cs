using System;
using System.Collections.Generic;
using System.Linq;

namespace TwinParticles.CheckEngine.Infrastructure.Vehicle.Vin;

public class VinWmiAllowList
{
    public VinWmiAllowList(IEnumerable<string> wmis)
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

public sealed class BmwVinWmiAllowList : VinWmiAllowList
{
    public BmwVinWmiAllowList(IEnumerable<string> wmis)
        : base(wmis)
    {
    }
}

public sealed class CatalogVinWmiAllowList : VinWmiAllowList
{
    public CatalogVinWmiAllowList(IEnumerable<string> wmis)
        : base(wmis)
    {
    }
}
