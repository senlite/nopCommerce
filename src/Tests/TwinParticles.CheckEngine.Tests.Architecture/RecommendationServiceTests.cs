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
    public async Task GetRecommendationsAsync_Should_Not_Use_Blank_Keyword_Search_When_Unscoped()
    {
        // Regression: the unscoped rail previously called keyword search with empty text, which the
        // production repository short-circuits to an empty list, so the rail never rendered.
        var repository = new RecordingRepository();
        var service = new RecommendationService(
            repository,
            new FitmentEvaluationService(new FakeFitmentRepository(), new FakeFitmentCache()));

        var recommendations = await service.GetRecommendationsAsync(null, take: 3, CancellationToken.None);

        repository.BrowseUnscopedCalls.Should().Be(1);
        repository.BlankKeywordSearchCalls.Should().Be(0);
        recommendations.VehicleScoped.Should().BeFalse();
        recommendations.Hits.Should().NotBeEmpty();
    }

    [Test]
    public async Task GetRecommendationsAsync_Should_Exclude_Seed_And_Prefer_Its_Category_When_Unscoped()
    {
        var service = new RecommendationService(
            new FakeRepository(),
            new FitmentEvaluationService(new FakeFitmentRepository(), new FakeFitmentCache()));

        var recommendations = await service.GetRecommendationsAsync(null, take: 2, CancellationToken.None, seedProductId: 1001);

        recommendations.Hits.Should().NotContain(x => x.ProductId == 1001);
        recommendations.Hits[0].ProductId.Should().Be(1003, "1003 shares category 10 with the seed product");
    }

    [Test]
    public async Task GetRecommendationsAsync_Should_Bound_Unscoped_Browse_Page_Size()
    {
        var repository = new RecordingRepository();
        var service = new RecommendationService(
            repository,
            new FitmentEvaluationService(new FakeFitmentRepository(), new FakeFitmentCache()));

        await service.GetRecommendationsAsync(null, take: 4, CancellationToken.None);

        repository.LastBrowsePageSize.Should().BeGreaterThan(0);
        repository.LastBrowsePageSize.Should().BeLessThanOrEqualTo(100);
    }

    [Test]
    public async Task GetRecommendationsAsync_Should_Preserve_SeName_For_Storefront_Links()
    {
        var service = new RecommendationService(
            new SeNameRepository(),
            new FitmentEvaluationService(new FakeFitmentRepository(), new FakeFitmentCache()));

        var recommendations = await service.GetRecommendationsAsync(777, take: 5, CancellationToken.None);

        recommendations.Hits.Should().ContainSingle();
        recommendations.Hits[0].SeName.Should().Be("oil-filter-bmw");
    }

    [Test]
    public async Task GetRecommendationsAsync_Should_Clamp_Take_To_At_Least_One()
    {
        var service = new RecommendationService(
            new FakeRepository(),
            new FitmentEvaluationService(new FakeFitmentRepository(), new FakeFitmentCache()));

        var recommendations = await service.GetRecommendationsAsync(777, take: 0, CancellationToken.None);

        recommendations.Hits.Should().HaveCount(1);
        recommendations.Hits[0].ProductId.Should().Be(1001);
    }

    [Test]
    public void GetRecommendationsAsync_Should_Not_Accept_Customer_Identifiers()
    {
        var method = typeof(RecommendationService).GetMethod(nameof(RecommendationService.GetRecommendationsAsync));
        method.Should().NotBeNull();
        method!.GetParameters().Select(p => p.Name ?? string.Empty)
            .Should().NotContain(name => name.Contains("customer", System.StringComparison.OrdinalIgnoreCase));
    }

    private sealed class RecordingRepository : FakeRepository
    {
        public int BrowseUnscopedCalls { get; private set; }

        public int BlankKeywordSearchCalls { get; private set; }

        public int LastBrowsePageSize { get; private set; }

        public override Task<IReadOnlyList<SearchHit>> BrowseUnscopedAsync(SearchQuery query, CancellationToken cancellationToken)
        {
            BrowseUnscopedCalls++;
            LastBrowsePageSize = query.PageSize;
            return base.BrowseUnscopedAsync(query, cancellationToken);
        }

        public override Task<IReadOnlyList<SearchHit>> SearchKeywordAsync(SearchQuery query, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(query.RawText))
                BlankKeywordSearchCalls++;

            return base.SearchKeywordAsync(query, cancellationToken);
        }
    }

    private sealed class SeNameRepository : FakeRepository
    {
        public override Task<IReadOnlyList<SearchHit>> SearchByVehicleTreeAsync(SearchQuery query, CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyList<SearchHit>>([
                new SearchHit { ProductId = 1001, Name = "Oil Filter", CategoryId = 10, Score = 0.9m, SeName = "oil-filter-bmw" }
            ]);
    }

    private class FakeRepository : IProductSearchReadRepository
    {
        private static IReadOnlyList<SearchHit> Catalog() =>
        [
            new SearchHit { ProductId = 1001, Name = "Oil Filter", CategoryId = 10, Score = 0.9m },
            new SearchHit { ProductId = 1002, Name = "Hose", CategoryId = 20, Score = 0.8m },
            new SearchHit { ProductId = 1003, Name = "Belt", CategoryId = 10, Score = 0.7m }
        ];

        // Mirrors the production contract: keyword search returns nothing for blank text. Without this
        // the double hides callers that pass empty text and silently get no recommendations.
        public virtual Task<IReadOnlyList<SearchHit>> SearchKeywordAsync(SearchQuery query, CancellationToken cancellationToken)
            => Task.FromResult(string.IsNullOrWhiteSpace(query.RawText)
                ? []
                : Catalog());

        public Task<IReadOnlyList<SearchHit>> SearchByCategoryAsync(SearchQuery query, CancellationToken cancellationToken)
            => Task.FromResult(Catalog());

        public virtual Task<IReadOnlyList<SearchHit>> BrowseUnscopedAsync(SearchQuery query, CancellationToken cancellationToken)
            => Task.FromResult(Catalog());

        public virtual Task<IReadOnlyList<SearchHit>> SearchByVehicleTreeAsync(SearchQuery query, CancellationToken cancellationToken)
            => Task.FromResult(Catalog());

        public Task<IReadOnlyList<SearchHit>> SearchByOemIdAsync(int oemNumberId, SearchQuery query, CancellationToken cancellationToken)
            => Task.FromResult(Catalog());
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
