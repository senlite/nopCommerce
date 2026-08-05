using System;
using System.Text;
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
public class ImportPipelineReviewStatusTests
{
    [Test]
    public async Task SetReviewStatus_Should_Validate_Status_And_Normalize_Casing()
    {
        var orchestrator = CreateOrchestrator();

        var run = await orchestrator.RunAsync(new ImportPipelineRunRequest
        {
            Format = ImportSourceFormat.Csv,
            FileName = "review.csv",
            Content = Encoding.UTF8.GetBytes("oem,name,vehicleConfigurationId\n11-51-7-586-925,Item,1001\n"),
            DryRun = true
        }, CancellationToken.None);

        orchestrator.SetReviewStatus(run.BatchId, 1, "bad-status").Should().BeFalse();
        orchestrator.SetReviewStatus(run.BatchId, 1, "approved").Should().BeTrue();

        var batch = orchestrator.GetBatch(run.BatchId);
        batch.Should().NotBeNull();
        batch!.Rows[0].ReviewStatus.Should().Be("Approved");

        orchestrator.SetReviewStatus(run.BatchId, 1, "rejected").Should().BeTrue();
        batch = orchestrator.GetBatch(run.BatchId);
        batch!.Rows[0].ReviewStatus.Should().Be("Rejected");
        batch.Rows[0].ReviewReasonCode.Should().Be("import.review.rejected_by_operator");
    }

    private static ImportPipelineOrchestratorService CreateOrchestrator()
    {
        var clock = new FakeClock();
        var extraction = new ImportExtractionService([new CsvImportExtractionParser()]);
        var normalization = new ImportNormalizationService(new FakeOemNormalizationService());
        var duplicate = new ImportDuplicateDetectionService();

        var resolveService = new OemResolveService(new FakeOemNormalizationService(), new FakeSearchRepository(), new OemSupersessionService(new EmptyRelationReadRepository()), new EmptyProductOemMapRepository());
        var oemMatching = new ImportOemMatchingService(resolveService);

        return new ImportPipelineOrchestratorService(
            clock,
            extraction,
            normalization,
            duplicate,
            oemMatching,
            new ImportVehicleMatchingService(),
            new ImportAiEnrichmentHookService(),
            new ImportTranslationHookService(),
            new ImportSeoGenerationHookService(),
            new ImportCategorizationService(),
            new ImportImageAssignmentService(),
            new ImportReviewService(),
            new ImportPublicationService(),
            new ImageImportOrchestrationService(new ProductImageService(new FakeImageStorageService(), new FakeImageDeliveryService(), new FakeImageQuarantineService(), new FakeProductImageRepository())));
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
        public Task<System.Collections.Generic.IReadOnlyList<OemNumber>> FindByNormalizedNumberAsync(string normalizedNumber, int? manufacturerId, CancellationToken cancellationToken)
        {
            System.Collections.Generic.IReadOnlyList<OemNumber> rows = [new OemNumber
            {
                Id = 10,
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
        public Task<System.Collections.Generic.IReadOnlyList<OemRelation>> GetActiveOutgoingRelationsAsync(int fromOemNumberId, CancellationToken cancellationToken)
            => Task.FromResult<System.Collections.Generic.IReadOnlyList<OemRelation>>([]);
    }

    private sealed class EmptyProductOemMapRepository : IProductOemMapRepository
    {
        public Task UpsertAsync(ProductOemMap map, CancellationToken cancellationToken)
            => Task.CompletedTask;

        public Task<System.Collections.Generic.IReadOnlyList<ProductOemMap>> GetByProductIdAsync(int productId, CancellationToken cancellationToken)
            => Task.FromResult<System.Collections.Generic.IReadOnlyList<ProductOemMap>>([]);

        public Task<System.Collections.Generic.IReadOnlyList<ProductOemMap>> GetByOemNumberIdAsync(int oemNumberId, CancellationToken cancellationToken)
            => Task.FromResult<System.Collections.Generic.IReadOnlyList<ProductOemMap>>([]);
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
}
