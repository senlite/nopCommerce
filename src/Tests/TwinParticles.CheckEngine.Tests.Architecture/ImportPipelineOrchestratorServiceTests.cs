using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using NUnit.Framework;
using TwinParticles.CheckEngine.Application.Images;
using TwinParticles.CheckEngine.Application.ImportPipeline.Extraction;
using TwinParticles.CheckEngine.Application.ImportPipeline.Normalization;
using TwinParticles.CheckEngine.Application.ImportPipeline.Orchestration;
using TwinParticles.CheckEngine.Application.ImportPipeline.Stages;
using TwinParticles.CheckEngine.Application.Oem;
using TwinParticles.CheckEngine.Domain.ImportPipeline;
using TwinParticles.CheckEngine.Domain.Oem;
using TwinParticles.CheckEngine.Domain.Performance;
using TwinParticles.CheckEngine.Infrastructure.ImportPipeline;

namespace TwinParticles.CheckEngine.Tests.Architecture;

[TestFixture]
public class ImportPipelineOrchestratorServiceTests
{
    [Test]
    public async Task RunAsync_Then_Publish_Should_Produce_Partial_Success_When_Some_Rows_Still_Pending_Review()
    {
        var orchestrator = CreateOrchestrator();

        var csv = "oem,name,vehicleConfigurationId,category,image\n11-51-7-586-925,Oil Filter,1001,Engine,https://img/1.jpg\n11-51-7-586-925,Oil Filter,1001,Engine,https://img/2.jpg\n";
        var run = await orchestrator.RunAsync(new ImportPipelineRunRequest
        {
            Format = ImportSourceFormat.Csv,
            FileName = "supplier.csv",
            Content = Encoding.UTF8.GetBytes(csv),
            DryRun = false
        }, CancellationToken.None);

        run.TotalRows.Should().Be(2);
        run.ReviewRows.Should().Be(1);

        var publish = orchestrator.Publish(run.BatchId, dryRun: false);

        publish.PublishedRows.Should().Be(1);
        publish.FailedRows.Should().Be(1);

        var batch = orchestrator.GetBatch(run.BatchId);
        batch.Should().NotBeNull();
        batch!.Status.Should().Be("Committed");
    }

    [Test]
    public async Task Sql_Authoritative_State_Should_Survive_Orchestrator_Restart()
    {
        var repository = new DurableTestRepository();
        var firstProcess = CreateOrchestrator(repository);
        var csv = "oem,name,vehicleConfigurationId,category,image\n11-51-7-586-925,Oil Filter,1001,Engine,https://img/1.jpg\n";

        var run = await firstProcess.RunAsync(new ImportPipelineRunRequest
        {
            Format = ImportSourceFormat.Csv,
            FileName = "restart.csv",
            Content = Encoding.UTF8.GetBytes(csv)
        }, CancellationToken.None);

        var secondProcess = CreateOrchestrator(repository);
        var restored = await secondProcess.GetBatchAsync(run.BatchId, CancellationToken.None);

        restored.Should().NotBeNull();
        restored!.Status.Should().Be("Review");
        restored.CurrentStage.Should().Be(nameof(ImportPipelineStage.Review));
        restored.CompletedStages.Should().HaveCount(11);
        restored.SourceContent.Should().Equal(Encoding.UTF8.GetBytes(csv));
        restored.Rows.Should().ContainSingle();
        restored.Rows[0].OemNumberId.Should().Be(10);
        restored.Rows[0].VehicleConfigurationId.Should().Be(1001);
        restored.Rows[0].Category.Should().Be("Engine");
        repository.BatchUpsertCount.Should().BeGreaterThanOrEqualTo(23,
            "the batch is checkpointed before and after each of eleven stages");

        (await secondProcess.SetReviewStatusAsync(
            run.BatchId,
            rowNumber: 1,
            reviewStatus: "Rejected",
            actor: "test",
            CancellationToken.None)).Should().BeTrue();

        var thirdProcess = CreateOrchestrator(repository);
        var afterReview = await thirdProcess.GetBatchAsync(run.BatchId, CancellationToken.None);
        afterReview!.Rows[0].ReviewStatus.Should().Be("Rejected");
    }

    [Test]
    public async Task Rerun_VehicleMatch_Should_Preserve_Oem_Result_And_Not_Reexecute_Oem_Match()
    {
        var repository = new DurableTestRepository();
        var search = new FakeSearchRepository();
        var firstProcess = CreateOrchestrator(repository, search);
        var run = await firstProcess.RunAsync(new ImportPipelineRunRequest
        {
            Format = ImportSourceFormat.Csv,
            FileName = "rerun.csv",
            Content = Encoding.UTF8.GetBytes(
                "oem,name,vehicleConfigurationId\n11-51-7-586-925,Oil Filter,invalid\n")
        }, CancellationToken.None);
        var oemCallsAfterInitialRun = search.CallCount;

        repository.SetRowField(run.BatchId, 1, "vehicleConfigurationId", "2002");

        var restartedProcess = CreateOrchestrator(repository, search);
        await restartedProcess.RerunStageAsync(
            run.BatchId,
            ImportPipelineStage.VehicleMatch,
            CancellationToken.None);

        var restored = await restartedProcess.GetBatchAsync(run.BatchId, CancellationToken.None);
        restored!.Rows[0].VehicleConfigurationId.Should().Be(2002);
        restored.Rows[0].VehicleMatchConfidence.Should().Be(1m);
        restored.Rows[0].OemNumberId.Should().Be(10);
        search.CallCount.Should().Be(oemCallsAfterInitialRun,
            "rerunning vehicle-match must not execute OEM-match again");
    }

    [Test]
    public async Task Ten_Thousand_High_Confidence_Rows_Should_Reach_Published_State_Above_50_Rows_Per_Second()
    {
        var csv = new StringBuilder("oem,name,sku,vehicleConfigurationId,category\n");
        for (var i = 1; i <= 10_000; i++)
            csv.Append("OEM-").Append(i).Append(",Part ").Append(i).Append(",SKU-").Append(i)
                .Append(",1001,Engine\n");

        var orchestrator = CreateOrchestrator();
        var stopwatch = Stopwatch.StartNew();
        var run = await orchestrator.RunAsync(new ImportPipelineRunRequest
        {
            Format = ImportSourceFormat.Csv,
            FileName = "reference-10000.csv",
            Content = Encoding.UTF8.GetBytes(csv.ToString()),
            DryRun = false
        }, CancellationToken.None);
        var publish = await orchestrator.PublishAsync(run.BatchId, dryRun: false, CancellationToken.None);
        stopwatch.Stop();

        var throughput = run.TotalRows / stopwatch.Elapsed.TotalSeconds;
        TestContext.WriteLine(
            $"10,000-row import+publication: {stopwatch.Elapsed.TotalSeconds:F3}s, {throughput:F1} rows/s");
        run.TotalRows.Should().Be(10_000);
        run.ReviewRows.Should().Be(0, "above-threshold rows require no manual data entry");
        publish.PublishedRows.Should().Be(10_000);
        publish.FailedRows.Should().Be(0);
        throughput.Should().BeGreaterThanOrEqualTo(50, "NFR-009 structured import throughput");
    }

    [TestCase(ImportPipelineStage.Extract)]
    [TestCase(ImportPipelineStage.Normalize)]
    [TestCase(ImportPipelineStage.Deduplicate)]
    [TestCase(ImportPipelineStage.OemMatch)]
    [TestCase(ImportPipelineStage.VehicleMatch)]
    [TestCase(ImportPipelineStage.Enrich)]
    [TestCase(ImportPipelineStage.Translate)]
    [TestCase(ImportPipelineStage.SeoGenerate)]
    [TestCase(ImportPipelineStage.Categorize)]
    [TestCase(ImportPipelineStage.ImageAssign)]
    [TestCase(ImportPipelineStage.Review)]
    [TestCase(ImportPipelineStage.Publish)]
    public async Task Every_Stage_Should_Be_Independently_Rerunnable_From_Durable_State(ImportPipelineStage stage)
    {
        var repository = new DurableTestRepository();
        var firstProcess = CreateOrchestrator(repository);
        var run = await firstProcess.RunAsync(new ImportPipelineRunRequest
        {
            Format = ImportSourceFormat.Csv,
            FileName = "all-stages.csv",
            Content = Encoding.UTF8.GetBytes(
                "oem,name,sku,vehicleConfigurationId\n11-51-7-586-925,Oil Filter,OF-1,1001\n")
        }, CancellationToken.None);

        var restartedProcess = CreateOrchestrator(repository);
        var result = await restartedProcess.RerunStageAsync(run.BatchId, stage, CancellationToken.None);

        result.Stage.Should().Be(stage);
        var restored = await restartedProcess.GetBatchAsync(run.BatchId, CancellationToken.None);
        restored.Should().NotBeNull();
        restored!.CompletedStages.Should().Contain(stage.ToString());
    }

    private static ImportPipelineOrchestratorService CreateOrchestrator(
        IImportPipelineRepository? repository = null,
        FakeSearchRepository? searchRepository = null)
    {
        var normalization = new FakeOemNormalizationService();
        var resolveService = new OemResolveService(
            normalization,
            searchRepository ?? new FakeSearchRepository(),
            new OemSupersessionService(new EmptyRelationReadRepository()),
            new EmptyProductOemMapRepository());

        return new ImportPipelineOrchestratorService(
            new FakeClock(),
            new ImportExtractionService([new CsvImportExtractionParser()]),
            new ImportNormalizationService(normalization),
            new ImportDuplicateDetectionService(),
            new ImportOemMatchingService(resolveService),
            new ImportVehicleMatchingService(),
            new ImportAiEnrichmentHookService(),
            new ImportTranslationHookService(),
            new ImportSeoGenerationHookService(),
            new ImportCategorizationService(),
            new ImportImageAssignmentService(),
            new ImportReviewService(),
            new ImportPublicationService(),
            new ImageImportOrchestrationService(new ProductImageService(
                new FakeImageStorageService(),
                new FakeImageDeliveryService(),
                new FakeImageQuarantineService(),
                new FakeProductImageRepository())),
            pipelineRepository: repository);
    }

    private sealed class FakeClock : ICheckEngineClock
    {
        public DateTimeOffset UtcNow { get; } = new(2026, 8, 3, 12, 0, 0, TimeSpan.Zero);
    }

    private sealed class FakeOemNormalizationService : IOemNormalizationService
    {
        public string Normalize(string rawNumber)
        {
            return rawNumber.Replace("-", string.Empty).Replace(" ", string.Empty).Trim().ToUpperInvariant();
        }
    }

    private sealed class FakeSearchRepository : IOemSearchReadRepository
    {
        public int CallCount { get; private set; }

        public Task<IReadOnlyList<OemNumber>> FindByNormalizedNumberAsync(string normalizedNumber, int? manufacturerId, CancellationToken cancellationToken)
        {
            CallCount++;
            var id = normalizedNumber == "11517586925"
                ? 10
                : Math.Abs(StringComparer.Ordinal.GetHashCode(normalizedNumber)) + 100;
            IReadOnlyList<OemNumber> rows = [new OemNumber
            {
                Id = id,
                ManufacturerId = 1,
                DisplayNumber = "11-51-7-586-925",
                NormalizedNumber = "11517586925",
                IsObsolete = false,
                IsActive = true
            }];

            return Task.FromResult(rows);
        }
    }

    private sealed class EmptyRelationReadRepository : IOemRelationReadRepository
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

    private sealed class FakeProductImageRepository : TwinParticles.CheckEngine.Domain.Images.IProductImageRepository
    {
        public Task<TwinParticles.CheckEngine.Domain.Images.ProductImageRecord?> GetPrimaryAsync(int productId, CancellationToken cancellationToken)
            => Task.FromResult<TwinParticles.CheckEngine.Domain.Images.ProductImageRecord?>(null);

        public Task UpsertPrimaryAsync(TwinParticles.CheckEngine.Domain.Images.ProductImageRecord record, CancellationToken cancellationToken)
            => Task.CompletedTask;
    }

    private sealed class FakeImageStorageService : TwinParticles.CheckEngine.Domain.Images.IImageStorageService
    {
        public Task<int?> DownloadAndCreatePictureAsync(string url, string seoName, string altText, string titleText, CancellationToken cancellationToken)
            => Task.FromResult<int?>(123);

        public Task<int?> GetDefaultPlaceholderPictureIdAsync(CancellationToken cancellationToken)
            => Task.FromResult<int?>(999);

        public Task<bool> ReplacePictureBinaryAsync(int pictureId, string sourceUrl, string seoName, string altText, string titleText, CancellationToken cancellationToken)
            => Task.FromResult(true);
    }

    private sealed class FakeImageDeliveryService : TwinParticles.CheckEngine.Domain.Images.IImageDeliveryService
    {
        public Task<string?> GetVariantUrlAsync(int pictureId, TwinParticles.CheckEngine.Domain.Images.ImageVariant variant, CancellationToken cancellationToken)
            => Task.FromResult<string?>("/images/123/product");
    }

    private sealed class FakeImageQuarantineService : TwinParticles.CheckEngine.Domain.Images.IImageQuarantineService
    {
        public Task<bool> ShouldQuarantineAsync(string sourceUrl, CancellationToken cancellationToken)
            => Task.FromResult(false);
    }

    private sealed class DurableTestRepository : IImportPipelineRepository
    {
        private readonly Dictionary<int, ImportBatch> _batches = [];
        private readonly Dictionary<int, List<ImportRow>> _rows = [];
        private int _nextId = 1;

        public int BatchUpsertCount { get; private set; }

        public Task<ImportBatch?> GetBatchAsync(int batchId, CancellationToken cancellationToken)
            => Task.FromResult(_batches.TryGetValue(batchId, out var batch) ? Clone(batch) : null);

        public Task<ImportBatch?> GetBatchByCorrelationIdAsync(Guid correlationId, CancellationToken cancellationToken)
        {
            var batch = _batches.Values.SingleOrDefault(item => item.CorrelationId == correlationId);
            return Task.FromResult(batch is null ? null : Clone(batch));
        }

        public Task UpsertBatchAsync(ImportBatch batch, CancellationToken cancellationToken)
        {
            BatchUpsertCount++;
            if (batch.Id <= 0)
                batch.Id = _nextId++;
            _batches[batch.Id] = Clone(batch);
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<ImportRow>> GetRowsAsync(int batchId, CancellationToken cancellationToken)
        {
            IReadOnlyList<ImportRow> rows = _rows.TryGetValue(batchId, out var values)
                ? values.Select(Clone).ToList()
                : [];
            return Task.FromResult(rows);
        }

        public Task ReplaceRowsAsync(int batchId, IReadOnlyList<ImportRow> rows, CancellationToken cancellationToken)
        {
            _rows[batchId] = rows.Select(Clone).ToList();
            return Task.CompletedTask;
        }

        public void SetRowField(Guid correlationId, int rowNumber, string key, string value)
        {
            var batch = _batches.Values.Single(item => item.CorrelationId == correlationId);
            var row = _rows[batch.Id].Single(item => item.RowNumber == rowNumber);
            var state = JsonSerializer.Deserialize<ImportPipelineRowState>(
                row.PipelineStateJson!,
                new JsonSerializerOptions(JsonSerializerDefaults.Web))!;
            var fields = new Dictionary<string, string?>(state.Fields, StringComparer.OrdinalIgnoreCase)
            {
                [key] = value
            };
            state.Fields = fields;
            row.RawPayload = JsonSerializer.Serialize(fields);
            row.PipelineStateJson = JsonSerializer.Serialize(
                state,
                new JsonSerializerOptions(JsonSerializerDefaults.Web));
        }

        private static ImportBatch Clone(ImportBatch value)
            => new()
            {
                Id = value.Id,
                CorrelationId = value.CorrelationId,
                FileName = value.FileName,
                SourceFormat = value.SourceFormat,
                Status = value.Status,
                UploadedByCustomerId = value.UploadedByCustomerId,
                RowCount = value.RowCount,
                ErrorSummary = value.ErrorSummary,
                SourceContent = value.SourceContent.ToArray(),
                RunOptionsJson = value.RunOptionsJson,
                CurrentStage = value.CurrentStage,
                CompletedStagesCsv = value.CompletedStagesCsv,
                CreatedUtc = value.CreatedUtc,
                UpdatedUtc = value.UpdatedUtc
            };

        private static ImportRow Clone(ImportRow value)
            => new()
            {
                Id = value.Id,
                BatchId = value.BatchId,
                RowNumber = value.RowNumber,
                RawPayload = value.RawPayload,
                NormalizedOem = value.NormalizedOem,
                MatchedOemNumberId = value.MatchedOemNumberId,
                ProposedProductId = value.ProposedProductId,
                ProposedFitmentJson = value.ProposedFitmentJson,
                Confidence = value.Confidence,
                ReviewStatus = value.ReviewStatus,
                ReviewNote = value.ReviewNote,
                PipelineStateJson = value.PipelineStateJson,
                CompletedStagesCsv = value.CompletedStagesCsv,
                LastStageError = value.LastStageError
            };
    }
}
