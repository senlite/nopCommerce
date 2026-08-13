using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using NUnit.Framework;
using TwinParticles.CheckEngine.Application.Erp;
using TwinParticles.CheckEngine.Application.Fitment;
using TwinParticles.CheckEngine.Application.ImportPipeline.Orchestration;
using TwinParticles.CheckEngine.Application.ImportPipeline.Stages;
using TwinParticles.CheckEngine.Application.Oem;
using TwinParticles.CheckEngine.Application.Search;
using TwinParticles.CheckEngine.Application.Vehicle.Vin;
using TwinParticles.CheckEngine.Domain.Ai;
using TwinParticles.CheckEngine.Domain.Erp;
using TwinParticles.CheckEngine.Domain.Fitment;
using TwinParticles.CheckEngine.Domain.Observability;
using TwinParticles.CheckEngine.Domain.Oem;
using TwinParticles.CheckEngine.Domain.Search;
using TwinParticles.CheckEngine.Domain.Vehicle;

namespace TwinParticles.CheckEngine.Tests.Architecture;

[TestFixture]
public class ResilienceBehaviorTests
{
    [Test]
    public async Task UnifiedSearch_Should_Set_IsDegraded_When_SearchIndex_Unhealthy()
    {
        var service = CreateSearchService(indexHealthy: false);

        var result = await service.SearchAsync(new SearchQuery
        {
            RawText = "bmw",
            Mode = SearchMode.Keyword,
            Locale = "en"
        }, CancellationToken.None);

        result.IsDegraded.Should().BeTrue();
    }

    [Test]
    public void ImportAiHook_Should_Not_Throw_When_AiCompletionPort_Fails()
    {
        var hook = new ImportAiEnrichmentHookService(new ThrowingAiCompletionPort());
        var rows = new List<ImportPipelineRowState>
        {
            new()
            {
                RowNumber = 1,
                Fields = new Dictionary<string, string?> { ["name"] = "Oil Filter" }
            }
        };

        Action act = () => hook.Apply(rows, enabled: true);

        act.Should().NotThrow();
        rows[0].Fields.Should().ContainKey("aiEnrichmentStatus");
        rows[0].Fields["aiEnrichmentStatus"].Should().Be("skipped");
    }

    [Test]
    public async Task StubErp_ForceFail_Job_Should_Return_False_Without_Throwing()
    {
        var adapter = new ForceFailErpAdapter();
        var queue = new InMemoryErpQueue();
        var service = new ErpSyncService(queue, adapter, new NoAutoResolve());

        await service.QueueSyncAsync(ErpSyncEntityType.Product, ErpSyncDirection.PushToErp, "SKU-1", "force-fail", CancellationToken.None);

        Func<Task> act = async () => await service.ProcessPendingAsync(CancellationToken.None);
        await act.Should().NotThrowAsync();

        var report = await service.BuildReconciliationReportAsync(CancellationToken.None);
        report.FailedJobs.Should().Be(1);
        report.SuccessfulJobs.Should().Be(0);
    }

    private static UnifiedSearchService CreateSearchService(bool indexHealthy)
    {
        var vinService = new VinDecodeApplicationService(new EmptyVinRegistry(), new NoopTelemetry());
        var oemService = new OemResolveService(
            new PassthroughOemNormalization(),
            new EmptyOemSearch(),
            new OemSupersessionService(new EmptyOemRelations()),
            new EmptyProductOemMap());
        var fitmentService = new FitmentEvaluationService(new EmptyFitmentRead(), new EmptyFitmentCache());

        return new UnifiedSearchService(
            new KeywordRepository(),
            vinService,
            oemService,
            fitmentService,
            new FakeSearchIndexHealthService(indexHealthy),
            new LowerNormalizer());
    }

    private sealed class ThrowingAiCompletionPort : IAiCompletionPort
    {
        public Task<AiCompletionResult> CompleteAsync(AiCompletionRequest request, CancellationToken ct)
            => throw new InvalidOperationException("ai.provider_down");
    }

    private sealed class ForceFailErpAdapter : IErpClientAdapter
    {
        public Task<bool> PushAsync(ErpSyncJob job, CancellationToken cancellationToken)
            => Task.FromResult(!job.Payload.Contains("force-fail"));

        public Task<string?> PullInventorySnapshotAsync(CancellationToken cancellationToken)
            => Task.FromResult<string?>("{\"items\":[]}");
    }

    private sealed class NoAutoResolve : IErpConflictResolutionService
    {
        public bool CanAutoResolve(ErpSyncJob job) => false;

        public void ApplyAutoResolution(ErpSyncJob job)
        {
        }
    }

    private sealed class InMemoryErpQueue : IErpSyncQueueRepository
    {
        private readonly List<ErpSyncJob> _jobs = [];

        public Task EnqueueAsync(ErpSyncJob job, CancellationToken cancellationToken)
        {
            _jobs.Add(job);
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<ErpSyncJob>> GetPendingAsync(CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyList<ErpSyncJob>>(_jobs.FindAll(x => x.Status == "Queued"));

        public Task<ErpSyncJob?> GetByIdAsync(Guid jobId, CancellationToken cancellationToken)
            => Task.FromResult(_jobs.Find(x => x.JobId == jobId));

        public Task UpdateAsync(ErpSyncJob job, CancellationToken cancellationToken)
        {
            _jobs.RemoveAll(x => x.JobId == job.JobId);
            _jobs.Add(job);
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<ErpSyncJob>> GetAllAsync(CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyList<ErpSyncJob>>(_jobs);
    }

    private sealed class FakeSearchIndexHealthService : ISearchIndexHealthService
    {
        private readonly bool _healthy;

        public FakeSearchIndexHealthService(bool healthy) => _healthy = healthy;

        public Task<bool> IsHealthyAsync(CancellationToken cancellationToken) => Task.FromResult(_healthy);

        public Task RebuildAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class LowerNormalizer : IBilingualSearchTextNormalizer
    {
        public string Normalize(string text, string locale) => (text ?? string.Empty).Trim().ToLowerInvariant();
    }

    private sealed class KeywordRepository : IProductSearchReadRepository
    {
        public Task<IReadOnlyList<SearchHit>> SearchKeywordAsync(SearchQuery query, CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyList<SearchHit>>([new SearchHit { ProductId = 1, Name = "BMW Filter", Score = 1m }]);

        public Task<IReadOnlyList<SearchHit>> SearchByCategoryAsync(SearchQuery query, CancellationToken cancellationToken)
            => SearchKeywordAsync(query, cancellationToken);

        public Task<IReadOnlyList<SearchHit>> SearchByVehicleTreeAsync(SearchQuery query, CancellationToken cancellationToken)
            => SearchKeywordAsync(query, cancellationToken);

        public Task<IReadOnlyList<SearchHit>> SearchByOemIdAsync(int oemNumberId, SearchQuery query, CancellationToken cancellationToken)
            => SearchKeywordAsync(query, cancellationToken);
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

    private sealed class PassthroughOemNormalization : IOemNormalizationService
    {
        public string Normalize(string rawNumber) => rawNumber?.Trim().ToUpperInvariant() ?? string.Empty;
    }

    private sealed class EmptyOemSearch : IOemSearchReadRepository
    {
        public Task<IReadOnlyList<OemNumber>> FindByNormalizedNumberAsync(string normalizedNumber, int? manufacturerId, CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyList<OemNumber>>([]);
    }

    private sealed class EmptyProductOemMap : IProductOemMapRepository
    {
        public Task UpsertAsync(ProductOemMap map, CancellationToken cancellationToken) => Task.CompletedTask;

        public Task<IReadOnlyList<ProductOemMap>> GetByProductIdAsync(int productId, CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyList<ProductOemMap>>([]);

        public Task<IReadOnlyList<ProductOemMap>> GetByOemNumberIdAsync(int oemNumberId, CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyList<ProductOemMap>>([]);
    }

    private sealed class EmptyOemRelations : IOemRelationReadRepository
    {
        public Task<IReadOnlyList<OemRelation>> GetActiveOutgoingRelationsAsync(int fromOemNumberId, CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyList<OemRelation>>([]);
    }

    private sealed class EmptyFitmentRead : IFitmentClaimReadRepository
    {
        public Task<IReadOnlyList<FitmentClaim>> GetClaimsAsync(int productId, int vehicleConfigurationId, CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyList<FitmentClaim>>([]);

        public Task<IReadOnlyList<FitmentClaim>> GetReviewQueueAsync(CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyList<FitmentClaim>>([]);
    }

    private sealed class EmptyFitmentCache : IFitmentCache
    {
        public Task<FitmentEvaluationResult?> GetAsync(FitmentEvaluationContext context, CancellationToken cancellationToken)
            => Task.FromResult<FitmentEvaluationResult?>(null);

        public Task SetAsync(FitmentEvaluationContext context, FitmentEvaluationResult result, CancellationToken cancellationToken)
            => Task.CompletedTask;

        public Task InvalidateAsync(int productId, int vehicleConfigurationId, CancellationToken cancellationToken)
            => Task.CompletedTask;
    }
}
