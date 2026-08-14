using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using TwinParticles.CheckEngine.Domain.Observability;
using TwinParticles.CheckEngine.Domain.Security;
using TwinParticles.CheckEngine.Domain.Vehicle;

namespace TwinParticles.CheckEngine.Application.Vehicle.Vin;

public sealed class VinDecodeApplicationService
{
    private static readonly JsonSerializerOptions AuditJsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly IVinDecoderRegistry _decoderRegistry;
    private readonly ICheckEngineTelemetry _telemetry;
    private readonly VinDecodeOptions _options;
    private readonly IVinPrivacyService? _privacyService;
    private readonly ICheckEngineAuditService? _auditService;

    public VinDecodeApplicationService(
        IVinDecoderRegistry decoderRegistry,
        ICheckEngineTelemetry telemetry,
        VinDecodeOptions? options = null,
        IVinPrivacyService? privacyService = null,
        ICheckEngineAuditService? auditService = null)
    {
        _decoderRegistry = decoderRegistry;
        _telemetry = telemetry;
        _options = options ?? VinDecodeOptions.Current;
        _privacyService = privacyService;
        _auditService = auditService;
    }

    public Task<VinDecodeResult> DecodeAsync(string rawVin, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!TwinParticles.CheckEngine.Domain.Vehicle.Vin.TryCreate(rawVin, out var vin, out var errorCode, enforceCheckDigit: true))
        {
            TrackDecode("Failed", wmi: null, normalizedVin: null, candidateCount: 0, reasonCode: errorCode);
            RecordAudit("Failed", wmi: null, normalizedVin: null, candidateCount: 0, reasonCode: errorCode, cancellationToken);

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
            TrackDecode("Failed", segments.Wmi, vin.Value, candidateCount: 0, reasonCode: "vin.wmi_unknown");
            RecordAudit("Failed", segments.Wmi, vin.Value, candidateCount: 0, reasonCode: "vin.wmi_unknown", cancellationToken);

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

        var outcome = ClassifyOutcome(orderedCandidates);
        var reasonCode = orderedCandidates.Count == 0
            ? contribution.ReasonCode ?? "vin.decode_failed"
            : null;

        TrackDecode(outcome, segments.Wmi, vin.Value, orderedCandidates.Count, reasonCode);
        RecordAudit(outcome, segments.Wmi, vin.Value, orderedCandidates.Count, reasonCode, cancellationToken);

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

    private string ClassifyOutcome(IReadOnlyList<VinDecodeCandidate> orderedCandidates)
    {
        if (orderedCandidates.Count == 0)
            return "Failed";

        if (orderedCandidates.Count > 1)
            return "NeedsDisambiguation";

        return orderedCandidates[0].Confidence.Value >= _options.AutoAcceptConfidenceThreshold
            ? "SingleMatch"
            : "NeedsDisambiguation";
    }

    private void TrackDecode(
        string outcome,
        string? wmi,
        string? normalizedVin,
        int candidateCount,
        string? reasonCode)
    {
        _telemetry.TrackEvent("checkengine.vin.decode", new Dictionary<string, object?>
        {
            ["outcome"] = outcome,
            ["wmi"] = wmi,
            ["candidateCount"] = candidateCount,
            ["reasonCode"] = reasonCode,
            ["vinLast4"] = _privacyService?.GetLast4(normalizedVin),
            ["vinHash"] = normalizedVin is null ? null : _privacyService?.CreateHash(normalizedVin)
        });
    }

    private void RecordAudit(
        string outcome,
        string? wmi,
        string? normalizedVin,
        int candidateCount,
        string? reasonCode,
        CancellationToken cancellationToken)
    {
        if (_auditService is null || _privacyService is null || normalizedVin is null)
            return;

        var payload = JsonSerializer.Serialize(new
        {
            outcome,
            wmi,
            vinLast4 = _privacyService.GetLast4(normalizedVin),
            vinHash = _privacyService.CreateHash(normalizedVin),
            candidateCount,
            reasonCode
        }, AuditJsonOptions);

        _ = _auditService.AppendAsync(
            actor: "system",
            action: "vin.decode",
            entityType: "VinDecode",
            entityId: _privacyService.CreateHash(normalizedVin),
            beforeJson: null,
            afterJson: payload,
            cancellationToken);
    }
}
