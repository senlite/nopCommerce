using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TwinParticles.CheckEngine.Domain.Fitment;

namespace TwinParticles.CheckEngine.Application.Fitment;

public sealed class FitmentEvaluationService
{
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
        var publishedClaims = claims.Where(x => x.IsPublished && x.IsActive).ToList();

        if (publishedClaims.Count == 0)
        {
            var unknown = new FitmentEvaluationResult
            {
                Outcome = FitmentStatus.Unknown,
                EffectiveConfidence = 0m,
                ReasonCode = "fitment.no_published_claim"
            };

            await _cache.SetAsync(context.ProductId, context.VehicleConfigurationId, unknown, cancellationToken);
            return unknown;
        }

        var prioritized = publishedClaims
            .OrderByDescending(x => x.Confidence)
            .ThenByDescending(x => x.Status == FitmentStatus.DoesNotFit)
            .ToList();

        var selected = prioritized[0];
        var effectiveConfidence = selected.Confidence;

        if (selected.Qualifier.ProductionFromYear.HasValue || selected.Qualifier.ProductionToYear.HasValue)
        {
            if (!context.ProductionYear.HasValue)
            {
                effectiveConfidence = Math.Max(0m, effectiveConfidence - 0.25m);
            }
            else if (!IsYearInRange(context.ProductionYear.Value, selected.Qualifier.ProductionFromYear, selected.Qualifier.ProductionToYear))
            {
                var notFit = new FitmentEvaluationResult
                {
                    Outcome = FitmentStatus.DoesNotFit,
                    EffectiveConfidence = effectiveConfidence,
                    ReasonCode = "fitment.production_year_out_of_range"
                };

                await _cache.SetAsync(context.ProductId, context.VehicleConfigurationId, notFit, cancellationToken);
                return notFit;
            }
        }

        var outcome = selected.Status switch
        {
            FitmentStatus.Fits => effectiveConfidence < 0.6m ? FitmentStatus.Unknown : FitmentStatus.Fits,
            FitmentStatus.DoesNotFit => FitmentStatus.DoesNotFit,
            _ => FitmentStatus.Unknown
        };

        var result = new FitmentEvaluationResult
        {
            Outcome = outcome,
            EffectiveConfidence = effectiveConfidence,
            ReasonCode = outcome == FitmentStatus.Unknown ? "fitment.insufficient_confidence" : null
        };

        await _cache.SetAsync(context.ProductId, context.VehicleConfigurationId, result, cancellationToken);
        return result;
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
