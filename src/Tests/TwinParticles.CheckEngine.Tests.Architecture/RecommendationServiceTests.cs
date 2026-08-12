using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using NUnit.Framework;
using TwinParticles.CheckEngine.Application.Fitment;
using TwinParticles.CheckEngine.Application.Search;
using TwinParticles.CheckEngine.Domain.Fitment;
using TwinParticles.CheckEngine.Domain.Search;

namespace TwinParticles.CheckEngine.Tests.Architecture;

[TestFixture]
public class RecommendationServiceTests
{
    [Test]
    public async Task GetRecommendationsAsync_Should_Return_Only_Fits()
    {
        var service = new RecommendationService(
            new FakeRepository(),
            new FitmentEvaluationService(new FakeFitmentRepository(), new FakeFitmentCache()));

        var recommendations = await service.GetRecommendationsAsync(777, take: 10, CancellationToken.None);

        recommendations.Should().HaveCount(1);
        recommendations[0].ProductId.Should().Be(1001);
        recommendations.Should().OnlyContain(x => x.FitsActiveContext);
    }

    [Test]
    public async Task GetRecommendationsAsync_Should_Exclude_Unknown_And_DoesNotFit()
    {
        var service = new RecommendationService(
            new FakeRepository(),
            new FitmentEvaluationService(new FakeFitmentRepository(), new FakeFitmentCache()));

        var recommendations = await service.GetRecommendationsAsync(777, take: 10, CancellationToken.None);

        recommendations.Select(x => x.ProductId).Should().NotContain(1002);
        recommendations.Select(x => x.ProductId).Should().NotContain(1003);
    }

    private sealed class FakeRepository : IProductSearchReadRepository
    {
        public Task<IReadOnlyList<SearchHit>> SearchKeywordAsync(SearchQuery query, CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyList<SearchHit>>([
                new SearchHit { ProductId = 1001, Name = "Oil Filter", Score = 0.9m },
                new SearchHit { ProductId = 1002, Name = "Hose", Score = 0.8m },
                new SearchHit { ProductId = 1003, Name = "Belt", Score = 0.7m }
            ]);

        public Task<IReadOnlyList<SearchHit>> SearchByCategoryAsync(SearchQuery query, CancellationToken cancellationToken)
            => SearchKeywordAsync(query, cancellationToken);

        public Task<IReadOnlyList<SearchHit>> SearchByVehicleTreeAsync(SearchQuery query, CancellationToken cancellationToken)
            => SearchKeywordAsync(query, cancellationToken);

        public Task<IReadOnlyList<SearchHit>> SearchByOemIdAsync(int oemNumberId, SearchQuery query, CancellationToken cancellationToken)
            => SearchKeywordAsync(query, cancellationToken);
    }

    private sealed class FakeFitmentRepository : IFitmentClaimReadRepository
    {
        public Task<IReadOnlyList<FitmentClaim>> GetClaimsAsync(int productId, int vehicleConfigurationId, CancellationToken cancellationToken)
        {
            var status = productId switch
            {
                1001 => FitmentStatus.Fits,
                1002 => FitmentStatus.Unknown,
                _ => FitmentStatus.DoesNotFit
            };

            return Task.FromResult<IReadOnlyList<FitmentClaim>>([
                new FitmentClaim
                {
                    Id = productId,
                    ProductId = productId,
                    VehicleConfigurationId = vehicleConfigurationId,
                    Status = status,
                    Confidence = 0.95m,
                    IsPublished = true,
                    IsActive = true
                }
            ]);
        }

        public Task<IReadOnlyList<FitmentClaim>> GetReviewQueueAsync(CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyList<FitmentClaim>>([]);
    }

    private sealed class FakeFitmentCache : IFitmentCache
    {
        public Task<FitmentEvaluationResult?> GetAsync(int productId, int vehicleConfigurationId, CancellationToken cancellationToken)
            => Task.FromResult<FitmentEvaluationResult?>(null);

        public Task SetAsync(int productId, int vehicleConfigurationId, FitmentEvaluationResult result, CancellationToken cancellationToken)
            => Task.CompletedTask;

        public Task InvalidateAsync(int productId, int vehicleConfigurationId, CancellationToken cancellationToken)
            => Task.CompletedTask;
    }
}
