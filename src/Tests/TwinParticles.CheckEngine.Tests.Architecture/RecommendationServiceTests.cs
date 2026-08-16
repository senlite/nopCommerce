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

        recommendations.VehicleScoped.Should().BeTrue();
        recommendations.Hits.Should().HaveCount(1);
        recommendations.Hits[0].ProductId.Should().Be(1001);
        recommendations.Hits.Should().OnlyContain(x => x.FitsActiveContext);
    }

    [Test]
    public async Task GetRecommendationsAsync_Should_Exclude_Unknown_And_DoesNotFit()
    {
        var service = new RecommendationService(
            new FakeRepository(),
            new FitmentEvaluationService(new FakeFitmentRepository(), new FakeFitmentCache()));

        var recommendations = await service.GetRecommendationsAsync(777, take: 10, CancellationToken.None);

        recommendations.Hits.Select(x => x.ProductId).Should().NotContain(1002);
        recommendations.Hits.Select(x => x.ProductId).Should().NotContain(1003);
    }

    [Test]
    public async Task GetRecommendationsAsync_Should_Prefer_Same_Category_As_Seed()
    {
        var service = new RecommendationService(
            new FakeRepository(),
            new FitmentEvaluationService(new AllFitsFitmentRepository(), new FakeFitmentCache()));

        var recommendations = await service.GetRecommendationsAsync(777, take: 2, CancellationToken.None, seedProductId: 1001);

        recommendations.Hits.Should().NotContain(x => x.ProductId == 1001);
        recommendations.Hits[0].ProductId.Should().Be(1003);
    }

    [Test]
    public async Task GetRecommendationsAsync_Should_Label_Unscoped_When_No_Vehicle()
    {
        var service = new RecommendationService(
            new FakeRepository(),
            new FitmentEvaluationService(new FakeFitmentRepository(), new FakeFitmentCache()));

        var recommendations = await service.GetRecommendationsAsync(null, take: 2, CancellationToken.None);

        recommendations.VehicleScoped.Should().BeFalse();
        recommendations.Hits.Should().NotBeEmpty();
    }

    [Test]
    public void GetRecommendationsAsync_Should_Not_Accept_Customer_Identifiers()
    {
        var method = typeof(RecommendationService).GetMethod(nameof(RecommendationService.GetRecommendationsAsync));
        method.Should().NotBeNull();
        method!.GetParameters().Select(p => p.Name ?? string.Empty)
            .Should().NotContain(name => name.Contains("customer", System.StringComparison.OrdinalIgnoreCase));
    }

    private sealed class FakeRepository : IProductSearchReadRepository
    {
        public Task<IReadOnlyList<SearchHit>> SearchKeywordAsync(SearchQuery query, CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyList<SearchHit>>([
                new SearchHit { ProductId = 1001, Name = "Oil Filter", CategoryId = 10, Score = 0.9m },
                new SearchHit { ProductId = 1002, Name = "Hose", CategoryId = 20, Score = 0.8m },
                new SearchHit { ProductId = 1003, Name = "Belt", CategoryId = 10, Score = 0.7m }
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
        public Task<FitmentEvaluationResult?> GetAsync(FitmentEvaluationContext context, CancellationToken cancellationToken)
            => Task.FromResult<FitmentEvaluationResult?>(null);

        public Task SetAsync(FitmentEvaluationContext context, FitmentEvaluationResult result, CancellationToken cancellationToken)
            => Task.CompletedTask;

        public Task InvalidateAsync(int productId, int vehicleConfigurationId, CancellationToken cancellationToken)
            => Task.CompletedTask;
    }

    private sealed class AllFitsFitmentRepository : IFitmentClaimReadRepository
    {
        public Task<IReadOnlyList<FitmentClaim>> GetClaimsAsync(int productId, int vehicleConfigurationId, CancellationToken cancellationToken)
        {
            return Task.FromResult<IReadOnlyList<FitmentClaim>>([
                new FitmentClaim
                {
                    Id = productId,
                    ProductId = productId,
                    VehicleConfigurationId = vehicleConfigurationId,
                    Status = FitmentStatus.Fits,
                    Confidence = 0.95m,
                    IsPublished = true,
                    IsActive = true
                }
            ]);
        }

        public Task<IReadOnlyList<FitmentClaim>> GetReviewQueueAsync(CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyList<FitmentClaim>>([]);
    }
}
