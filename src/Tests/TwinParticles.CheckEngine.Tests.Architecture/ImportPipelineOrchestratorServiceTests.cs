using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using NUnit.Framework;
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
        var clock = new FakeClock();
        var extraction = new ImportExtractionService([new CsvImportExtractionParser()]);
        var normalization = new ImportNormalizationService(new FakeOemNormalizationService());
        var duplicate = new ImportDuplicateDetectionService();

        var resolveService = new OemResolveService(new FakeOemNormalizationService(), new FakeSearchRepository(), new OemSupersessionService(new EmptyRelationReadRepository()));
        var oemMatching = new ImportOemMatchingService(resolveService);

        var orchestrator = new ImportPipelineOrchestratorService(
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
            new ImportPublicationService());

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
        public Task<IReadOnlyList<OemNumber>> FindByNormalizedNumberAsync(string normalizedNumber, int? manufacturerId, CancellationToken cancellationToken)
        {
            IReadOnlyList<OemNumber> rows = [new OemNumber
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
        public Task<IReadOnlyList<OemRelation>> GetActiveOutgoingRelationsAsync(int fromOemNumberId, CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyList<OemRelation>>([]);
    }
}
