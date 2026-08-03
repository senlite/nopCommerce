using System.Collections.Generic;
using TwinParticles.CheckEngine.Domain.Vehicle;

namespace TwinParticles.CheckEngine.Application.Vehicle.Vin;

public sealed class VinDecodeResult
{
    public required string Outcome { get; init; }

    public string? NormalizedVin { get; init; }

    public bool? CheckDigitValid { get; init; }

    public string? Wmi { get; init; }

    public IReadOnlyList<VinDecodeCandidate> Candidates { get; init; } = [];

    public string? ReasonCode { get; init; }
}
