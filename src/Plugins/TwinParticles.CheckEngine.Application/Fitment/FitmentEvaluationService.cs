using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TwinParticles.CheckEngine.Domain.Fitment;

namespace TwinParticles.CheckEngine.Application.Fitment;

/// <summary>
/// Resolves whether a product fits a vehicle configuration.
///
/// The engine fails closed: any ambiguity resolves to Unknown or NeedsDisambiguation,
/// never to Fits (FR-302, AC-026.1, AC-15.5). Only published, active claims are ever
/// customer-visible (FR-303, FR-326), and a negative claim outranks any positive claim
/// regardless of confidence.
/// </summary>
public sealed class FitmentEvaluationService
{
    /// <summary>A positive claim below this effective confidence is reported as Unknown.</summary>
    public const decimal FitsConfidenceThreshold = 0.6m;

    private readonly IFitmentCache _cache;
    private readonly IFitmentClaimReadRepository _readRepository;

    public FitmentEvaluationService(IFitmentClaimReadRepository readRepository, IFitmentCache cache)
    {
        _readRepository = readRepository;
        _cache = cache;
    }

    public async Task<FitmentEvaluationResult> EvaluateAsync(FitmentEvaluationContext context, CancellationToken cancellationToken)
    {
        var cached = await _cache.GetAsync(context.ProductId, context.VehicleConfigurationId, cancellationToken);
        if (cached is not null)
            return cached;

        var claims = await _readRepository.GetClaimsAsync(context.ProductId, context.VehicleConfigurationId, cancellationToken);
        var result = Resolve(claims, context);

        await _cache.SetAsync(context.ProductId, context.VehicleConfigurationId, result, cancellationToken);
        return result;
    }

    private static FitmentEvaluationResult Resolve(IReadOnlyList<FitmentClaim> claims, FitmentEvaluationContext context)
    {
        var visible = claims
            .Where(claim => claim.IsPublished && claim.IsActive)
            .Where(claim => claim.Status is FitmentStatus.Fits or FitmentStatus.DoesNotFit)
            .Select(claim => new EvaluatedClaim(claim, QualifierMatch.Evaluate(claim.Qualifier, context)))
            .ToList();

        if (visible.Count == 0)
            return Unknown("fitment.no_published_claim");

        // A negative claim wins over any positive claim, however confident. A negative claim
        // whose qualifiers cannot be checked still applies, because clearing it would require
        // information the customer has not supplied.
        var negatives = visible
            .Where(x => x.Claim.Status == FitmentStatus.DoesNotFit && x.Match.Kind != QualifierMatchKind.Mismatch)
            .ToList();

        if (negatives.Count > 0)
            return new FitmentEvaluationResult
            {
                Outcome = FitmentStatus.DoesNotFit,
                EffectiveConfidence = negatives.Max(x => x.Claim.Confidence),
                ReasonCode = "fitment.negative_claim"
            };

        var positives = visible
            .Where(x => x.Claim.Status == FitmentStatus.Fits)
            .ToList();

        var applicable = positives.Where(x => x.Match.Kind == QualifierMatchKind.Match).ToList();
        if (applicable.Count > 0)
        {
            var confidence = applicable.Max(x => x.Claim.Confidence);

            return confidence >= FitsConfidenceThreshold
                ? new FitmentEvaluationResult { Outcome = FitmentStatus.Fits, EffectiveConfidence = confidence }
                : new FitmentEvaluationResult
                {
                    Outcome = FitmentStatus.Unknown,
                    EffectiveConfidence = confidence,
                    ReasonCode = "fitment.insufficient_confidence"
                };
        }

        // Nothing applies outright. Prefer asking the customer for the missing detail over
        // declaring a mismatch we cannot substantiate.
        var indeterminate = positives.FirstOrDefault(x => x.Match.Kind == QualifierMatchKind.Indeterminate);
        if (indeterminate is not null)
            return new FitmentEvaluationResult
            {
                Outcome = FitmentStatus.NeedsDisambiguation,
                EffectiveConfidence = 0m,
                ReasonCode = indeterminate.Match.ReasonCode
            };

        var mismatched = positives.FirstOrDefault(x => x.Match.Kind == QualifierMatchKind.Mismatch);
        if (mismatched is not null)
            return new FitmentEvaluationResult
            {
                Outcome = FitmentStatus.DoesNotFit,
                EffectiveConfidence = mismatched.Claim.Confidence,
                ReasonCode = mismatched.Match.ReasonCode
            };

        return Unknown("fitment.no_published_claim");
    }

    private static FitmentEvaluationResult Unknown(string reasonCode)
        => new() { Outcome = FitmentStatus.Unknown, EffectiveConfidence = 0m, ReasonCode = reasonCode };

    private sealed record EvaluatedClaim(FitmentClaim Claim, QualifierMatch Match);

    private enum QualifierMatchKind
    {
        /// <summary>Every stated qualifier is satisfied by the vehicle context.</summary>
        Match,

        /// <summary>The context contradicts a stated qualifier.</summary>
        Mismatch,

        /// <summary>The context does not carry a qualifier the claim depends on.</summary>
        Indeterminate
    }

    private sealed record QualifierMatch(QualifierMatchKind Kind, string? ReasonCode)
    {
        private static readonly QualifierMatch Matched = new(QualifierMatchKind.Match, null);

        public static QualifierMatch Evaluate(FitmentClaimQualifier qualifier, FitmentEvaluationContext context)
        {
            // A contradiction is stronger evidence than a gap, so mismatches are reported first
            // even when another qualifier is also unknown.
            string? missingReason = null;

            if (qualifier.ProductionFromYear.HasValue || qualifier.ProductionToYear.HasValue)
            {
                if (!context.ProductionYear.HasValue)
                    missingReason ??= "fitment.production_year_required";
                else if (!IsYearInRange(context.ProductionYear.Value, qualifier.ProductionFromYear, qualifier.ProductionToYear))
                    return new QualifierMatch(QualifierMatchKind.Mismatch, "fitment.production_year_out_of_range");
            }

            foreach (var (required, actual, mismatchReason, missingFieldReason) in Constraints(qualifier, context))
            {
                if (string.IsNullOrWhiteSpace(required))
                    continue;

                if (string.IsNullOrWhiteSpace(actual))
                    missingReason ??= missingFieldReason;
                else if (!string.Equals(required.Trim(), actual.Trim(), StringComparison.OrdinalIgnoreCase))
                    return new QualifierMatch(QualifierMatchKind.Mismatch, mismatchReason);
            }

            return missingReason is null ? Matched : new QualifierMatch(QualifierMatchKind.Indeterminate, missingReason);
        }

        private static IEnumerable<(string? Required, string? Actual, string MismatchReason, string MissingReason)> Constraints(
            FitmentClaimQualifier qualifier,
            FitmentEvaluationContext context)
        {
            yield return (qualifier.SteeringSide, context.SteeringSide,
                "fitment.steering_side_mismatch", "fitment.steering_side_required");
            yield return (qualifier.MarketRegion, context.MarketRegion,
                "fitment.market_region_mismatch", "fitment.market_region_required");
            yield return (qualifier.DriveType, context.DriveType,
                "fitment.drive_type_mismatch", "fitment.drive_type_required");
            yield return (qualifier.TransmissionType, context.TransmissionType,
                "fitment.transmission_type_mismatch", "fitment.transmission_type_required");
        }

        private static bool IsYearInRange(int year, int? from, int? to)
        {
            if (from.HasValue && year < from.Value)
                return false;

            if (to.HasValue && year > to.Value)
                return false;

            return true;
        }
    }
}
