using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TwinParticles.CheckEngine.Domain.Vehicle;
using VinValue = TwinParticles.CheckEngine.Domain.Vehicle.Vin;
using VinInfrastructure = TwinParticles.CheckEngine.Infrastructure.Vehicle.Vin;

namespace TwinParticles.CheckEngine.Infrastructure.Vehicle.VinDecoders;

public sealed class BmwVinDecoder : IManufacturerVinDecoder
{
    private readonly VinInfrastructure.BmwVinWmiAllowList _wmiAllowList;
    private readonly IVinSupportRepository _vinRepository;
    private readonly VinInfrastructure.BmwVinConfigurationResolver _configurationResolver;

    public BmwVinDecoder(
        VinInfrastructure.BmwVinWmiAllowList wmiAllowList,
        IVinSupportRepository vinRepository,
        VinInfrastructure.BmwVinConfigurationResolver configurationResolver)
    {
        _wmiAllowList = wmiAllowList;
        _vinRepository = vinRepository;
        _configurationResolver = configurationResolver;
    }

    public bool CanDecode(string wmi)
        => _wmiAllowList.Contains(wmi);

    public VinDecodeContribution Decode(VinValue vin)
        => DecodeAsync(vin, CancellationToken.None).ConfigureAwait(false).GetAwaiter().GetResult();

    public async Task<VinDecodeContribution> DecodeAsync(VinValue vin, CancellationToken cancellationToken)
    {
        var segments = vin.ParseSegments();
        if (!CanDecode(segments.Wmi))
            return VinDecodeContribution.Failed("vin.wmi_unknown");

        var vdsPrefix = segments.Vds[..3];
        var patterns = (await _vinRepository.GetPatternsAsync(cancellationToken))
            .Where(pattern => pattern.IsActive && string.Equals(pattern.Pattern, vdsPrefix, StringComparison.Ordinal))
            .OrderByDescending(pattern => pattern.Priority)
            .ToList();

        if (patterns.Count == 0)
            return VinDecodeContribution.Failed("vin.decode_failed");

        var modelYear = VinModelYear.DecodeFromVin(vin.Value);
        var candidates = new List<VinDecodeCandidate>();

        foreach (var pattern in patterns)
        {
            var resolved = await _configurationResolver.ResolveAsync(pattern, modelYear, cancellationToken);
            candidates.AddRange(resolved);
        }

        var distinct = candidates
            .GroupBy(candidate => candidate.VehicleConfigurationId)
            .Select(group => group.OrderByDescending(candidate => candidate.Confidence.Value).First())
            .OrderByDescending(candidate => candidate.Confidence.Value)
            .ToList();

        return distinct.Count == 0
            ? VinDecodeContribution.Failed("vin.decode_failed")
            : VinDecodeContribution.WithCandidates(distinct);
    }
}
