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

    public async Task<RecommendationResult> GetRecommendationsAsync(
        int? vehicleConfigurationId,
        int take,
        CancellationToken cancellationToken,
        int? seedProductId = null)
    {
        var pageSize = Math.Max(1, take);

        if (!vehicleConfigurationId.HasValue)
        {
            var hits = await _productSearchReadRepository.SearchKeywordAsync(new SearchQuery
            {
                RawText = string.Empty,
                Mode = SearchMode.Keyword,
                WidenFitment = true,
                Page = 1,
                PageSize = Math.Max(pageSize * 4, 24)
            }, cancellationToken);

            return new RecommendationResult
            {
                VehicleScoped = false,
                Hits = Rank(hits, seedCategoryId: null).Take(pageSize).ToList()
            };
        }

        var vehicleHits = await _productSearchReadRepository.SearchByVehicleTreeAsync(new SearchQuery
        {
            VehicleConfigurationId = vehicleConfigurationId,
            Mode = SearchMode.VehicleTree,
            Page = 1,
            PageSize = Math.Max(pageSize * 4, 24)
        }, cancellationToken);

        var seedCategoryId = seedProductId.HasValue
            ? vehicleHits.FirstOrDefault(hit => hit.ProductId == seedProductId.Value)?.CategoryId
            : null;

        var fitting = new List<SearchHit>();
        foreach (var hit in vehicleHits.OrderByDescending(x => x.Score).ThenBy(x => x.ProductId))
        {
            if (seedProductId.HasValue && hit.ProductId == seedProductId.Value)
                continue;

            var fitment = await _fitmentEvaluationService.EvaluateAsync(new FitmentEvaluationContext
            {
                ProductId = hit.ProductId,
                VehicleConfigurationId = vehicleConfigurationId.Value
            }, cancellationToken);

            if (fitment.Outcome != FitmentStatus.Fits)
                continue;

            hit.FitsActiveContext = true;
            fitting.Add(hit);

            if (fitting.Count >= pageSize * 2)
                break;
        }

        return new RecommendationResult
        {
            VehicleScoped = true,
            Hits = Rank(fitting, seedCategoryId).Take(pageSize).ToList()
        };
    }

    private static IEnumerable<SearchHit> Rank(IEnumerable<SearchHit> hits, int? seedCategoryId)
    {
        return hits
            .OrderByDescending(hit => seedCategoryId.HasValue && hit.CategoryId == seedCategoryId ? 1 : 0)
            .ThenByDescending(hit => hit.Score)
            .ThenBy(hit => hit.ProductId);
    }
}
