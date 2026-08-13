using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using NUnit.Framework;
using TwinParticles.CheckEngine.Application.Fitment;
using TwinParticles.CheckEngine.Application.Oem;
using TwinParticles.CheckEngine.Application.Search;
using TwinParticles.CheckEngine.Application.Vehicle.Vin;
using TwinParticles.CheckEngine.Domain.Fitment;
using TwinParticles.CheckEngine.Domain.Observability;
using TwinParticles.CheckEngine.Domain.Oem;
using TwinParticles.CheckEngine.Domain.Search;
using TwinParticles.CheckEngine.Domain.Vehicle;

namespace TwinParticles.CheckEngine.Tests.Architecture;

[TestFixture]
public class GarageContextSearchServiceTests
{
    [Test]
    public async Task SearchWithGarageContextAsync_Should_Apply_Active_Vehicle_When_Query_Lacks_Context()
    {
        var service = CreateService();

        var result = await service.SearchWithGarageContextAsync(new SearchQuery
        {
            RawText = "filter",
            Mode = SearchMode.Keyword,
            Locale = "en"
        }, activeVehicleConfigurationId: 777, CancellationToken.None);

        result.Hits.Should().ContainSingle(x => x.ProductId == 2001);
        result.Hits.Should().NotContain(x => x.ProductId == 1001);
    }

    [Test]
    public async Task SearchWithGarageContextAsync_Should_Preserve_Explicit_Context()
    {
        var service = CreateService();

        var result = await service.SearchWithGarageContextAsync(new SearchQuery
        {
            RawText = "filter",
            Mode = SearchMode.Keyword,
            VehicleConfigurationId = 888,
            Locale = "en"
        }, activeVehicleConfigurationId: 777, CancellationToken.None);

        result.Hits.Should().ContainSingle(x => x.ProductId == 1001);
        result.Hits.Should().NotContain(x => x.ProductId == 2001);
    }

    private static GarageContextSearchService CreateService()
    {
        var searchService = new UnifiedSearchService(
            new FakeSearchRepository(),
            new VinDecodeApplicationService(new FakeVinRegistry(), new NoopTelemetry()),
            new OemResolveService(new FakeOemNormalizationService(), new FakeOemSearchRepository(), new OemSupersessionService(new FakeOemRelationRepository()), new EmptyProductOemMapRepository()),
            new FitmentEvaluationService(new FakeFitmentRepository(), new FakeFitmentCache()),
            new FakeSearchIndexHealthService(),
            new LowerNormalizer());

        return new GarageContextSearchService(searchService);
    }

    private sealed class FakeSearchRepository : IProductSearchReadRepository
    {
        public Task<IReadOnlyList<SearchHit>> SearchKeywordAsync(SearchQuery query, CancellationToken cancellationToken)
        {
            return Task.FromResult<IReadOnlyList<SearchHit>>([
                new SearchHit { ProductId = 1001, Name = "Generic Filter", CategoryId = 10, Score = 0.8m },
                new SearchHit { ProductId = 2001, Name = "Active Vehicle Filter", CategoryId = 10, Score = 0.9m }
            ]);
        }

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
            if (productId == 2001 && vehicleConfigurationId == 777)
            {
                return Task.FromResult<IReadOnlyList<FitmentClaim>>([
                    new FitmentClaim
                    {
                        Id = 1,
                        ProductId = productId,
                        VehicleConfigurationId = vehicleConfigurationId,
                        Status = FitmentStatus.Fits,
                        Confidence = 0.95m,
                        IsPublished = true,
                        IsActive = true
                    }
                ]);
            }

            if (productId == 1001 && vehicleConfigurationId == 888)
            {
                return Task.FromResult<IReadOnlyList<FitmentClaim>>([
                    new FitmentClaim
                    {
                        Id = 2,
                        ProductId = productId,
                        VehicleConfigurationId = vehicleConfigurationId,
                        Status = FitmentStatus.Fits,
                        Confidence = 0.95m,
                        IsPublished = true,
                        IsActive = true
                    }
                ]);
            }

            return Task.FromResult<IReadOnlyList<FitmentClaim>>([
                new FitmentClaim
                {
                    Id = 3,
                    ProductId = productId,
                    VehicleConfigurationId = vehicleConfigurationId,
                    Status = FitmentStatus.DoesNotFit,
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

    private sealed class FakeVinRegistry : IVinDecoderRegistry
    {
        public IManufacturerVinDecoder? Resolve(string wmi) => null;
    }

    private sealed class NoopTelemetry : ICheckEngineTelemetry
    {
        public void TrackEvent(string eventName, IReadOnlyDictionary<string, object?> properties)
        {
        }
    }

    private sealed class FakeOemNormalizationService : IOemNormalizationService
    {
        public string Normalize(string rawNumber) => string.Empty;
    }

    private sealed class FakeOemSearchRepository : IOemSearchReadRepository
    {
        public Task<IReadOnlyList<OemNumber>> FindByNormalizedNumberAsync(string normalizedNumber, int? manufacturerId, CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyList<OemNumber>>([]);
    }

    private sealed class FakeOemRelationRepository : IOemRelationReadRepository
    {
        public Task<IReadOnlyList<OemRelation>> GetActiveOutgoingRelationsAsync(int fromOemNumberId, CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyList<OemRelation>>([]);
    }

    private sealed class EmptyProductOemMapRepository : IProductOemMapRepository
    {
        public Task UpsertAsync(ProductOemMap map, CancellationToken cancellationToken)
            => Task.CompletedTask;

        public Task<IReadOnlyList<ProductOemMap>> GetByProductIdAsync(int productId, CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyList<ProductOemMap>>([]);

        public Task<IReadOnlyList<ProductOemMap>> GetByOemNumberIdAsync(int oemNumberId, CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyList<ProductOemMap>>([]);
    }

    private sealed class FakeSearchIndexHealthService : ISearchIndexHealthService
    {
        public Task<bool> IsHealthyAsync(CancellationToken cancellationToken) => Task.FromResult(true);

        public Task ReportDegradedAsync(string reason, CancellationToken cancellationToken) => Task.CompletedTask;

        public Task RebuildAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class LowerNormalizer : IBilingualSearchTextNormalizer
    {
        public string Normalize(string text, string locale) => (text ?? string.Empty).Trim().ToLowerInvariant();
    }
}
