using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TwinParticles.CheckEngine.Domain.Observability;
using TwinParticles.CheckEngine.Domain.Vehicle;

namespace TwinParticles.CheckEngine.Application.Vehicle.Vin;

public sealed class VinDecodeApplicationService
{
    private readonly IVinDecoderRegistry _decoderRegistry;
    private readonly ICheckEngineTelemetry _telemetry;

    public VinDecodeApplicationService(IVinDecoderRegistry decoderRegistry, ICheckEngineTelemetry telemetry)
    {
        _decoderRegistry = decoderRegistry;
        _telemetry = telemetry;
    }

    public Task<VinDecodeResult> DecodeAsync(string rawVin, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!TwinParticles.CheckEngine.Domain.Vehicle.Vin.TryCreate(rawVin, out var vin, out var errorCode, enforceCheckDigit: true))
        {
            _telemetry.TrackEvent("checkengine.vin.decode", new Dictionary<string, object?>
            {
                ["outcome"] = "Failed",
                ["wmi"] = null,
                ["candidateCount"] = 0,
                ["reasonCode"] = errorCode
            });

            return Task.FromResult(new VinDecodeResult
            {
                Outcome = "Failed",
                CheckDigitValid = errorCode == "vin.check_digit_failed" ? false : null,
                ReasonCode = errorCode
            });
        }

        var segments = vin!.ParseSegments();
        var decoder = _decoderRegistry.Resolve(segments.Wmi);

        if (decoder is null)
        {
            _telemetry.TrackEvent("checkengine.vin.decode", new Dictionary<string, object?>
            {
                ["outcome"] = "Failed",
                ["wmi"] = segments.Wmi,
                ["candidateCount"] = 0,
                ["reasonCode"] = "vin.wmi_unknown"
            });

            return Task.FromResult(new VinDecodeResult
            {
                Outcome = "Failed",
                NormalizedVin = vin.Value,
                CheckDigitValid = true,
                Wmi = segments.Wmi,
                ReasonCode = "vin.wmi_unknown"
            });
        }

        VinDecodeContribution contribution;

        try
        {
            contribution = decoder.Decode(vin);
        }
        catch (Exception)
        {
            contribution = VinDecodeContribution.Failed("vin.decode_failed");
        }

        var orderedCandidates = contribution.Candidates
            .OrderByDescending(candidate => candidate.Confidence.Value)
            .ToList();

        var outcome = orderedCandidates.Count switch
        {
            0 => "Failed",
            1 => "SingleMatch",
            _ => "NeedsDisambiguation"
        };

        var reasonCode = orderedCandidates.Count == 0
            ? contribution.ReasonCode ?? "vin.decode_failed"
            : null;

        _telemetry.TrackEvent("checkengine.vin.decode", new Dictionary<string, object?>
        {
            ["outcome"] = outcome,
            ["wmi"] = segments.Wmi,
            ["candidateCount"] = orderedCandidates.Count,
            ["reasonCode"] = reasonCode
        });

        return Task.FromResult(new VinDecodeResult
        {
            Outcome = outcome,
            NormalizedVin = vin.Value,
            CheckDigitValid = true,
            Wmi = segments.Wmi,
            Candidates = orderedCandidates,
            ReasonCode = reasonCode
        });
    }
}
