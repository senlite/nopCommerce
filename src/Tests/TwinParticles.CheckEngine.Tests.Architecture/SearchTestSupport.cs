using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TwinParticles.CheckEngine.Application.Fitment;
using TwinParticles.CheckEngine.Application.Oem;
using TwinParticles.CheckEngine.Application.Search;
using TwinParticles.CheckEngine.Application.Vehicle.Aliases.Services;
using TwinParticles.CheckEngine.Application.Vehicle.Vin;
using TwinParticles.CheckEngine.Domain.Fitment;
using TwinParticles.CheckEngine.Domain.Observability;
using TwinParticles.CheckEngine.Domain.Oem;
using TwinParticles.CheckEngine.Domain.Performance;
using TwinParticles.CheckEngine.Domain.Search;
using TwinParticles.CheckEngine.Domain.Security;
using TwinParticles.CheckEngine.Domain.Vehicle;
using TwinParticles.CheckEngine.Domain.Vehicle.Aliases;
using TwinParticles.CheckEngine.Infrastructure.Search;
using TwinParticles.CheckEngine.Infrastructure.Vehicle.Aliases;

namespace TwinParticles.CheckEngine.Tests.Architecture;

/// <summary>
/// Shared builders and fakes for search unit/benchmark tests so each fixture does not re-declare the
/// full dependency graph.
/// </summary>
internal static class SearchTestSupport
{
    public static UnifiedSearchService BuildSearchService(
        IProductSearchReadRepository? repository = null,
        Dictionary<int, FitmentStatus>? fitmentMap = null,
        IReadOnlyList<OemNumber>? oemMatches = null,
        bool indexHealthy = true)
    {
        var vinService = new VinDecodeApplicationService(new EmptyVinRegistry(), new NoopTelemetry());
        var oemService = new OemResolveService(
            new LowerOemNormalizer(),
            new FakeOemSearchReadRepository(oemMatches ?? []),
            new OemSupersessionService(new EmptyOemRelationReadRepository()),
            new EmptyProductOemMapRepository());
        var fitmentService = new FitmentEvaluationService(new FakeFitmentReadRepository(fitmentMap), new FakeFitmentCache());

        return new UnifiedSearchService(
            repository ?? new InMemoryProductSearchReadRepository(),
            vinService,
            oemService,
            fitmentService,
            new FakeSearchIndexHealthService(indexHealthy),
            new DefaultBilingualSearchTextNormalizer());
    }

    public static SearchAutocompleteService BuildAutocompleteService(
        IEnumerable<VehicleAlias>? aliases = null,
        IReadOnlyList<OemNumber>? oemPrefixMatches = null,
        IProductSearchReadRepository? repository = null)
    {
        var aliasRepository = new InMemoryVehicleAliasRepository();
        if (aliases is not null)
        {
            foreach (var alias in aliases)
                aliasRepository.UpsertAsync(alias, CancellationToken.None).GetAwaiter().GetResult();
        }

        var aliasService = new VehicleAliasApplicationService(
            aliasRepository,
            aliasRepository,
            new MemoryVehicleAliasCache(),
            new VehicleAliasNormalizationService(),
            new PassthroughSanitizer(),
            new FixedClock(),
            new NoopTelemetry());

        return new SearchAutocompleteService(
            repository ?? new InMemoryProductSearchReadRepository(),
            new FakeOemPrefixRepository(oemPrefixMatches ?? []),
            new LowerOemNormalizer(),
            aliasService,
            new DefaultBilingualSearchTextNormalizer());
    }

    private sealed class FakeSearchIndexHealthService : ISearchIndexHealthService
    {
        private readonly bool _healthy;

        public FakeSearchIndexHealthService(bool healthy) => _healthy = healthy;

        public Task<bool> IsHealthyAsync(CancellationToken cancellationToken) => Task.FromResult(_healthy);

        public Task ReportDegradedAsync(string reason, CancellationToken cancellationToken) => Task.CompletedTask;

        public Task RebuildAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class EmptyVinRegistry : IVinDecoderRegistry
    {
        public IManufacturerVinDecoder? Resolve(string wmi) => null;
    }

    private sealed class NoopTelemetry : ICheckEngineTelemetry
    {
        public void TrackEvent(string eventName, IReadOnlyDictionary<string, object?> properties)
        {
        }
    }

    private sealed class LowerOemNormalizer : IOemNormalizationService
    {
        public string Normalize(string rawNumber)
            => string.IsNullOrWhiteSpace(rawNumber) ? string.Empty : rawNumber.Replace("-", string.Empty).Replace(" ", string.Empty).ToLowerInvariant();
    }

    private sealed class FakeOemSearchReadRepository : IOemSearchReadRepository
    {
        private readonly IReadOnlyList<OemNumber> _matches;

        public FakeOemSearchReadRepository(IReadOnlyList<OemNumber> matches) => _matches = matches;

        public Task<IReadOnlyList<OemNumber>> FindByNormalizedNumberAsync(string normalizedNumber, int? manufacturerId, CancellationToken cancellationToken)
            => Task.FromResult(_matches);
    }

    private sealed class FakeOemPrefixRepository : IOemSearchReadRepository
    {
        private readonly IReadOnlyList<OemNumber> _matches;

        public FakeOemPrefixRepository(IReadOnlyList<OemNumber> matches) => _matches = matches;

        public Task<IReadOnlyList<OemNumber>> FindByNormalizedNumberAsync(string normalizedNumber, int? manufacturerId, CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyList<OemNumber>>([]);

        public Task<IReadOnlyList<OemNumber>> FindByNormalizedPrefixAsync(string normalizedPrefix, int take, CancellationToken cancellationToken)
            => Task.FromResult(_matches);
    }

    private sealed class EmptyOemRelationReadRepository : IOemRelationReadRepository
    {
        public Task<IReadOnlyList<OemRelation>> GetActiveOutgoingRelationsAsync(int fromOemNumberId, CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyList<OemRelation>>([]);
    }

    private sealed class EmptyProductOemMapRepository : IProductOemMapRepository
    {
        public Task UpsertAsync(ProductOemMap map, CancellationToken cancellationToken) => Task.CompletedTask;

        public Task<IReadOnlyList<ProductOemMap>> GetByProductIdAsync(int productId, CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyList<ProductOemMap>>([]);

        public Task<IReadOnlyList<ProductOemMap>> GetByOemNumberIdAsync(int oemNumberId, CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyList<ProductOemMap>>([]);
    }

    private sealed class FakeFitmentReadRepository : IFitmentClaimReadRepository
    {
        private readonly Dictionary<int, FitmentStatus>? _fitmentByProduct;

        public FakeFitmentReadRepository(Dictionary<int, FitmentStatus>? fitmentByProduct) => _fitmentByProduct = fitmentByProduct;

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

    private sealed class PassthroughSanitizer : ICheckEngineInputSanitizer
    {
        public string SanitizeAlias(string value) => value;
    }

    private sealed class FixedClock : ICheckEngineClock
    {
        public System.DateTimeOffset UtcNow { get; } = new(2026, 8, 13, 12, 0, 0, System.TimeSpan.Zero);
    }
}
