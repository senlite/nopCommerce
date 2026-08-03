using System;
using System.Collections.Generic;
using TwinParticles.CheckEngine.Domain.Vehicle;

namespace TwinParticles.CheckEngine.Infrastructure.Vehicle.VinDecoders;

public sealed class BmwVinDecoder : IManufacturerVinDecoder
{
    private static readonly string[] SupportedWmis = ["WBA", "WBS", "WBX", "5UX", "5YM"];

    private static readonly Dictionary<string, VinDecodeCandidate> VdsPatternCandidates = new(StringComparer.Ordinal)
    {
        ["8E9"] = new VinDecodeCandidate
        {
            VehicleConfigurationId = 10041,
            Confidence = Confidence.Create(0.92m),
            ModelYear = 2016
        },
        ["3A5"] = new VinDecodeCandidate
        {
            VehicleConfigurationId = 10011,
            Confidence = Confidence.Create(0.88m),
            ModelYear = 2013
        }
    };

    public bool CanDecode(string wmi)
    {
        if (string.IsNullOrWhiteSpace(wmi))
        {
            return false;
        }

        var normalized = wmi.Trim().ToUpperInvariant();
        foreach (var supportedWmi in SupportedWmis)
        {
            if (string.Equals(normalized, supportedWmi, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    public VinDecodeContribution Decode(Vin vin)
    {
        var segments = vin.ParseSegments();
        if (!CanDecode(segments.Wmi))
        {
            return VinDecodeContribution.Failed("vin.wmi_unknown");
        }

        var vdsPrefix = segments.Vds[..3];
        if (VdsPatternCandidates.TryGetValue(vdsPrefix, out var candidate))
        {
            return VinDecodeContribution.WithCandidates([candidate]);
        }

        return VinDecodeContribution.Failed("vin.decode_failed");
    }
}
