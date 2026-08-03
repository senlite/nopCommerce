using System;
using System.Collections.Generic;

namespace TwinParticles.CheckEngine.Domain.Vehicle;

public sealed class VinDecodeContribution
{
    private static readonly IReadOnlyList<VinDecodeCandidate> EmptyCandidates = Array.Empty<VinDecodeCandidate>();

    private VinDecodeContribution(IReadOnlyList<VinDecodeCandidate> candidates, string? reasonCode)
    {
        Candidates = candidates;
        ReasonCode = reasonCode;
    }

    public IReadOnlyList<VinDecodeCandidate> Candidates { get; }

    public string? ReasonCode { get; }

    public static VinDecodeContribution WithCandidates(IReadOnlyList<VinDecodeCandidate> candidates)
    {
        return new VinDecodeContribution(candidates, null);
    }

    public static VinDecodeContribution Failed(string reasonCode)
    {
        return new VinDecodeContribution(EmptyCandidates, reasonCode);
    }
}
