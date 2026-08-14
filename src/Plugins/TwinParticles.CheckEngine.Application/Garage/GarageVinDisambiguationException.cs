using System;
using System.Collections.Generic;
using TwinParticles.CheckEngine.Domain.Vehicle;

namespace TwinParticles.CheckEngine.Application.Garage;

public sealed class GarageVinDisambiguationException : Exception
{
    public GarageVinDisambiguationException(IReadOnlyList<VinDecodeCandidate> candidates)
        : base("garage.vin_disambiguation_required")
    {
        Candidates = candidates;
    }

    public IReadOnlyList<VinDecodeCandidate> Candidates { get; }
}
