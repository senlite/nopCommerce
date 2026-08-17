using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using NUnit.Framework;
using TwinParticles.CheckEngine.Application.Ai;
using TwinParticles.CheckEngine.Application.ImportPipeline.Orchestration;
using TwinParticles.CheckEngine.Application.ImportPipeline.Stages;
using TwinParticles.CheckEngine.Domain.Ai;
using TwinParticles.CheckEngine.Domain.ImportPipeline;

namespace TwinParticles.CheckEngine.Tests.Architecture;

[TestFixture]
public class ImportPublicationCandidateRebindTests
{
    [Test]
    public async Task PublishAsync_Should_Rebind_Pending_Candidates_To_Published_Product_Id()
    {
        var store = new InMemoryGenerationRepository();
        var rebind = new AiImportCandidateRebindService(store);
        var publisher = new FakePublisher();
        var service = new ImportPublicationService(publisher, rebind);

        await store.InsertAsync(new AiGenerationCandidate
        {
            EntityType = AiGenerationEntityType.Translation,
            EntityId = 10,
            FeatureKey = AiFeatureKeys.ImportTranslation,
            Locale = "ar",
            OutputText = "فلتر",
            PromptKey = AiFeatureKeys.ImportTranslation,
            PromptHash = "hash",
            ReviewStatus = "pending"
        }, CancellationToken.None);

        var rows = new List<ImportPipelineRowState>
        {
            new()
            {
                RowNumber = 10,
                ReviewStatus = "Approved",
                Fields = new Dictionary<string, string?> { ["name"] = "Oil Filter" }
            }
        };

        var result = await service.PublishAsync(rows, dryRun: false, CancellationToken.None);

        result.PublishedRows.Should().Be(1);
        rows[0].Fields["publishedProductId"].Should().Be("501");
        store.Items.Single().EntityId.Should().Be(501);
    }

    private sealed class FakePublisher : IImportProductPublisher
    {
        public Task<ImportProductPublishResult> PublishAsync(
            IReadOnlyDictionary<string, string?> fields,
            int? oemNumberId,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(new ImportProductPublishResult
            {
                Success = true,
                ProductId = 501
            });
        }
    }

    private sealed class InMemoryGenerationRepository : IAiGenerationRepository
    {
        public List<AiGenerationCandidate> Items { get; } = [];

        public Task<int> InsertAsync(AiGenerationCandidate candidate, CancellationToken cancellationToken)
        {
            candidate.Id = Items.Count + 1;
            Items.Add(candidate);
            return Task.FromResult(candidate.Id);
        }

        public Task<AiGenerationCandidate?> GetByIdAsync(int id, CancellationToken cancellationToken) =>
            Task.FromResult(Items.FirstOrDefault(x => x.Id == id));

        public Task<IReadOnlyList<AiGenerationCandidate>> GetPendingByEntityAsync(
            AiGenerationEntityType entityType,
            int entityId,
            CancellationToken cancellationToken)
        {
            return Task.FromResult<IReadOnlyList<AiGenerationCandidate>>(
                Items.Where(x => x.EntityType == entityType && x.EntityId == entityId && x.ReviewStatus == "pending").ToList());
        }

        public Task MarkReviewedAsync(int id, bool approved, string reviewer, CancellationToken cancellationToken) =>
            Task.CompletedTask;

        public Task RebindImportRowEntityAsync(int importRowNumber, int productId, CancellationToken cancellationToken)
        {
            foreach (var candidate in Items.Where(x => x.EntityId == importRowNumber && x.ReviewStatus == "pending"))
                candidate.EntityId = productId;

            return Task.CompletedTask;
        }
    }
}
