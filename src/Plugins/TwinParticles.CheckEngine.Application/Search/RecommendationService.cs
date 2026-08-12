using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TwinParticles.CheckEngine.Application.Fitment;
using TwinParticles.CheckEngine.Domain.Fitment;
using TwinParticles.CheckEngine.Domain.Search;

namespace TwinParticles.CheckEngine.Application.Search;

public sealed class RecommendationService
{
    private readonly FitmentEvaluationService _fitmentEvaluationService;
    private readonly IProductSearchReadRepository _productSearchReadRepository;

    public RecommendationService(
        IProductSearchReadRepository productSearchReadRepository,
        FitmentEvaluationService fitmentEvaluationService)
    {
        _productSearchReadRepository = productSearchReadRepository;
        _fitmentEvaluationService = fitmentEvaluationService;
    }

    public async Task<IReadOnlyList<SearchHit>> GetRecommendationsAsync(
        int? vehicleConfigurationId,
        int take,
        CancellationToken cancellationToken)
    {
        var pageSize = Math.Max(1, take);

        IReadOnlyList<SearchHit> hits;
        if (vehicleConfigurationId.HasValue)
        {
            hits = await _productSearchReadRepository.SearchByVehicleTreeAsync(new SearchQuery
            {
                VehicleConfigurationId = vehicleConfigurationId,
                Mode = SearchMode.VehicleTree,
                Page = 1,
                PageSize = Math.Max(pageSize * 4, 24)
            }, cancellationToken);
        }
        else
        {
            hits = await _productSearchReadRepository.SearchKeywordAsync(new SearchQuery
            {
                RawText = string.Empty,
                Mode = SearchMode.Keyword,
                WidenFitment = true,
                Page = 1,
                PageSize = Math.Max(pageSize * 4, 24)
            }, cancellationToken);

            return hits.Take(pageSize).ToList();
        }

        var fitting = new List<SearchHit>();
        foreach (var hit in hits)
        {
            var fitment = await _fitmentEvaluationService.EvaluateAsync(new FitmentEvaluationContext
            {
                ProductId = hit.ProductId,
                VehicleConfigurationId = vehicleConfigurationId.Value
            }, cancellationToken);

            if (fitment.Outcome != FitmentStatus.Fits)
                continue;

            hit.FitsActiveContext = true;
            fitting.Add(hit);

            if (fitting.Count >= pageSize)
                break;
        }

        return fitting;
    }
}
