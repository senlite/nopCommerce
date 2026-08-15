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
public class UnifiedSearchServiceTests
{
    [Test]
    public async Task SearchAsync_Should_Use_Vin_Mode_For_Valid_Vin_Input()
    {
        var repository = new VinRecordingRepository();
        var service = CreateService(repository: repository);

        var result = await service.SearchAsync(new SearchQuery
        {
            RawText = "1HGCM82633A004352",
            Mode = SearchMode.Auto,
            Locale = "en"
        }, CancellationToken.None);

        result.ModeUsed.Should().Be(SearchMode.Vin);
        result.Hits.Should().NotBeEmpty();
        repository.VehicleTreeCalls.Should().Be(1,
            "a VIN resolves vehicle context and must search the fitment projection");
        repository.KeywordCalls.Should().Be(0,
            "a 17-character VIN is not a product keyword");
    }

    [Test]
    public async Task SearchAsync_Should_Use_Oem_Mode_When_Oem_Resolves()
    {
        var service = CreateService(oemMatches: [new OemNumber { Id = 101, ManufacturerId = 1, DisplayNumber = "11-51-7-586-925", NormalizedNumber = "11517586925" }]);

        var result = await service.SearchAsync(new SearchQuery
        {
            RawText = "11-51-7-586-925",
            Mode = SearchMode.Auto,
            Locale = "en"
        }, CancellationToken.None);

        result.ModeUsed.Should().Be(SearchMode.Oem);
        result.Hits.Should().NotBeEmpty();
    }

    [Test]
    public async Task SearchAsync_Should_Filter_By_Fitment_And_Include_Unknown_When_Widened()
    {
        var service = CreateService(fitmentMap: new Dictionary<int, FitmentStatus>
        {
            [1001] = FitmentStatus.Fits,
            [1002] = FitmentStatus.Unknown,
            [1003] = FitmentStatus.DoesNotFit
        });

        var strict = await service.SearchAsync(new SearchQuery
        {
            RawText = "",
            Mode = SearchMode.Keyword,
            Locale = "en",
            VehicleConfigurationId = 222,
            WidenFitment = false
        }, CancellationToken.None);

        strict.Hits.Should().HaveCount(1);
        strict.Hits[0].ProductId.Should().Be(1001);

        var widened = await service.SearchAsync(new SearchQuery
        {
            RawText = "",
            Mode = SearchMode.Keyword,
            Locale = "en",
            VehicleConfigurationId = 222,
            WidenFitment = true
        }, CancellationToken.None);

        widened.Hits.Should().HaveCount(2);
        widened.Hits.Should().Contain(x => x.ProductId == 1001);
        widened.Hits.Should().Contain(x => x.ProductId == 1002);
    }

    [Test]
    public async Task SearchAsync_Should_Fallback_To_Keyword_When_Index_Degraded_And_Primary_Mode_Empty()
    {
        var service = CreateService(
            oemMatches: [new OemNumber { Id = 101, ManufacturerId = 1, DisplayNumber = "X", NormalizedNumber = "x" }],
            repository: new EmptyOemRepository(),
            indexHealthy: false);

        var result = await service.SearchAsync(new SearchQuery
        {
            RawText = "bmw",
            Mode = SearchMode.Oem,
            Locale = "en"
        }, CancellationToken.None);

        result.IsDegraded.Should().BeTrue();
        result.Hits.Should().NotBeEmpty();
    }

    [Test]
    public async Task SearchAsync_Should_Return_Suggestions_On_Zero_Results()
    {
        var service = CreateService(repository: new EmptyRepository());

        var result = await service.SearchAsync(new SearchQuery
        {
            RawText = "no-hit-token",
            Mode = SearchMode.Keyword,
            Locale = "en",
            WidenFitment = false
        }, CancellationToken.None);

        result.Hits.Should().BeEmpty();
        result.Suggestions.Should().NotBeEmpty();
    }

    private static UnifiedSearchService CreateService(
        IReadOnlyList<OemNumber>? oemMatches = null,
        Dictionary<int, FitmentStatus>? fitmentMap = null,
        IProductSearchReadRepository? repository = null,
        bool indexHealthy = true)
    {
        var vinService = new VinDecodeApplicationService(new FakeVinRegistry(), new NoopTelemetry());
        var oemService = new OemResolveService(new FakeOemNormalizationService(), new FakeOemSearchReadRepository(oemMatches ?? []), new OemSupersessionService(new FakeOemRelationReadRepository()), new EmptyProductOemMapRepository());
        var fitmentService = new FitmentEvaluationService(new FakeFitmentReadRepository(fitmentMap), new FakeFitmentCache());

        return new UnifiedSearchService(
            repository ?? new DefaultRepository(),
            vinService,
            oemService,
            fitmentService,
            new FakeSearchIndexHealthService(indexHealthy),
            new LowerNormalizer(),
            new NaturalLanguageIntentParser());
    }

    private sealed class LowerNormalizer : IBilingualSearchTextNormalizer
    {
        public string Normalize(string text, string locale) => (text ?? string.Empty).Trim().ToLowerInvariant();
    }

    private sealed class FakeSearchIndexHealthService : ISearchIndexHealthService
    {
        private readonly bool _healthy;

        public FakeSearchIndexHealthService(bool healthy)
        {
            _healthy = healthy;
        }

        public Task<bool> IsHealthyAsync(CancellationToken cancellationToken) => Task.FromResult(_healthy);

        public Task ReportDegradedAsync(string reason, CancellationToken cancellationToken) => Task.CompletedTask;

        public Task RebuildAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private class DefaultRepository : IProductSearchReadRepository
    {
        public Task<IReadOnlyList<SearchHit>> SearchKeywordAsync(SearchQuery query, CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyList<SearchHit>>([
                new SearchHit { ProductId = 1001, Name = "BMW Oil Filter", CategoryId = 10, Score = 0.9m },
                new SearchHit { ProductId = 1002, Name = "Radiator Hose", CategoryId = 20, Score = 0.8m },
                new SearchHit { ProductId = 1003, Name = "Cabin Filter", CategoryId = 10, Score = 0.7m }
            ]);

        public Task<IReadOnlyList<SearchHit>> SearchByCategoryAsync(SearchQuery query, CancellationToken cancellationToken)
            => SearchKeywordAsync(query, cancellationToken);

        public Task<IReadOnlyList<SearchHit>> SearchByVehicleTreeAsync(SearchQuery query, CancellationToken cancellationToken)
            => SearchKeywordAsync(query, cancellationToken);

        public virtual Task<IReadOnlyList<SearchHit>> SearchByOemIdAsync(int oemNumberId, SearchQuery query, CancellationToken cancellationToken)
            => SearchKeywordAsync(query, cancellationToken);
    }

    private sealed class EmptyRepository : IProductSearchReadRepository
    {
        public Task<IReadOnlyList<SearchHit>> SearchKeywordAsync(SearchQuery query, CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyList<SearchHit>>([]);

        public Task<IReadOnlyList<SearchHit>> SearchByCategoryAsync(SearchQuery query, CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyList<SearchHit>>([]);

        public Task<IReadOnlyList<SearchHit>> SearchByVehicleTreeAsync(SearchQuery query, CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyList<SearchHit>>([]);

        public Task<IReadOnlyList<SearchHit>> SearchByOemIdAsync(int oemNumberId, SearchQuery query, CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyList<SearchHit>>([]);
    }

    private sealed class VinRecordingRepository : IProductSearchReadRepository
    {
        public int KeywordCalls { get; private set; }
        public int VehicleTreeCalls { get; private set; }

        public Task<IReadOnlyList<SearchHit>> SearchKeywordAsync(SearchQuery query, CancellationToken cancellationToken)
        {
            KeywordCalls++;
            return Task.FromResult<IReadOnlyList<SearchHit>>([]);
        }

        public Task<IReadOnlyList<SearchHit>> SearchByCategoryAsync(SearchQuery query, CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyList<SearchHit>>([]);

        public Task<IReadOnlyList<SearchHit>> SearchByVehicleTreeAsync(SearchQuery query, CancellationToken cancellationToken)
        {
            VehicleTreeCalls++;
            query.VehicleConfigurationId.Should().Be(444);
            return Task.FromResult<IReadOnlyList<SearchHit>>([
                new SearchHit { ProductId = 1, Name = "VIN-fit product", Score = 1m }
            ]);
        }

        public Task<IReadOnlyList<SearchHit>> SearchByOemIdAsync(int oemNumberId, SearchQuery query, CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyList<SearchHit>>([]);
    }

    private sealed class EmptyOemRepository : DefaultRepository
    {
        public override Task<IReadOnlyList<SearchHit>> SearchByOemIdAsync(int oemNumberId, SearchQuery query, CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyList<SearchHit>>([]);
    }

    private sealed class FakeVinRegistry : IVinDecoderRegistry
    {
        public IManufacturerVinDecoder? Resolve(string wmi)
        {
            if (wmi is "WBA" or "1HG")
                return new FakeVinDecoder();

            return null;
        }
    }

    private sealed class FakeVinDecoder : IManufacturerVinDecoder
    {
        public bool CanDecode(string wmi) => wmi == "WBA";

        public VinDecodeContribution Decode(Vin vin)
        {
            return VinDecodeContribution.WithCandidates([
                new VinDecodeCandidate
                {
                    VehicleConfigurationId = 444,
                    Confidence = Confidence.Create(0.95m),
                    ModelYear = 2016
                }
            ]);
        }
    }

    private sealed class NoopTelemetry : ICheckEngineTelemetry
    {
        public void TrackEvent(string eventName, IReadOnlyDictionary<string, object?> properties)
        {
        }
    }

    private sealed class FakeOemNormalizationService : IOemNormalizationService
    {
        public string Normalize(string rawNumber)
            => string.IsNullOrWhiteSpace(rawNumber) ? string.Empty : rawNumber.Replace("-", string.Empty).ToLowerInvariant();
    }

    private sealed class FakeOemSearchReadRepository : IOemSearchReadRepository
    {
        private readonly IReadOnlyList<OemNumber> _matches;

        public FakeOemSearchReadRepository(IReadOnlyList<OemNumber> matches)
        {
            _matches = matches;
        }

        public Task<IReadOnlyList<OemNumber>> FindByNormalizedNumberAsync(string normalizedNumber, int? manufacturerId, CancellationToken cancellationToken)
        {
            var matches = _matches;
            return Task.FromResult(matches);
        }
    }

    private sealed class FakeOemRelationReadRepository : IOemRelationReadRepository
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

    private sealed class FakeFitmentReadRepository : IFitmentClaimReadRepository
    {
        private readonly Dictionary<int, FitmentStatus>? _fitmentByProduct;

        public FakeFitmentReadRepository(Dictionary<int, FitmentStatus>? fitmentByProduct)
        {
            _fitmentByProduct = fitmentByProduct;
        }

        public Task<IReadOnlyList<FitmentClaim>> GetClaimsAsync(int productId, int vehicleConfigurationId, CancellationToken cancellationToken)
        {
            if (_fitmentByProduct is null || !_fitmentByProduct.TryGetValue(productId, out var status))
                return Task.FromResult<IReadOnlyList<FitmentClaim>>([]);

            return Task.FromResult<IReadOnlyList<FitmentClaim>>([
                new FitmentClaim
                {
                    Id = productId,
                    ProductId = productId,
                    VehicleConfigurationId = vehicleConfigurationId,
                    Status = status,
                    Confidence = status == FitmentStatus.Unknown ? 0.8m : 0.95m,
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
}
