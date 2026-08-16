using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using NUnit.Framework;
using TwinParticles.CheckEngine.Application.Ai;
using TwinParticles.CheckEngine.Domain.Ai;

namespace TwinParticles.CheckEngine.Tests.Architecture;

[TestFixture]
public class AiContentCandidateServiceTests
{
    [Test]
    public async Task ReviewAsync_Should_Approve_And_Apply_Pending_Candidate()
    {
        var store = new InMemoryGenerationRepository();
        var applicator = new RecordingApplicator();
        var service = new AiContentCandidateService(store, applicator);

        var id = await service.SaveCandidateAsync(
            AiGenerationEntityType.ProductDescription,
            7,
            AiFeatureKeys.ImportEnrichment,
            "en",
            "candidate text",
            AiFeatureKeys.ImportEnrichment,
            "hash",
            0.8m,
            CancellationToken.None);

        var reviewed = await service.ReviewAsync(id!.Value, approved: true, "nour", CancellationToken.None);
        var stored = await store.GetByIdAsync(id.Value, CancellationToken.None);

        reviewed.Success.Should().BeTrue();
        stored!.ReviewStatus.Should().Be("approved");
        stored.IsPublished.Should().BeTrue();
        applicator.Applied.Should().BeTrue();
        applicator.LastCandidate!.OutputText.Should().Be("candidate text");
    }

    [Test]
    public async Task ReviewAsync_Should_Reject_Glossary_Invalid_Translation_Approval()
    {
        var store = new InMemoryGenerationRepository();
        var applicator = new RecordingApplicator();
        var service = new AiContentCandidateService(store, applicator);

        var id = await service.SaveCandidateAsync(
            AiGenerationEntityType.Translation,
            7,
            AiFeatureKeys.ImportTranslation,
            "ar",
            "مضخة BMW",
            AiFeatureKeys.ImportTranslation,
            "hash",
            0.5m,
            CancellationToken.None);

        var reviewed = await service.ReviewAsync(id!.Value, approved: true, "nour", CancellationToken.None);
        var stored = await store.GetByIdAsync(id.Value, CancellationToken.None);

        reviewed.Success.Should().BeFalse();
        reviewed.ReasonCode.Should().Be("ai.review.glossary_invalid");
        stored!.ReviewStatus.Should().Be("pending");
        applicator.Applied.Should().BeFalse();
    }

    [Test]
    public async Task ReviewAsync_Should_Reject_Specification_Approval_When_Unknown_Keys_Present()
    {
        var store = new InMemoryGenerationRepository();
        var applicator = new RecordingApplicator();
        var service = new AiContentCandidateService(store, applicator);

        var id = await service.SaveCandidateAsync(
            AiGenerationEntityType.Specification,
            7,
            AiFeatureKeys.ImportSpecification,
            "en",
            "Material: Steel\nFooBar: X",
            AiFeatureKeys.ImportSpecification,
            "hash",
            0.5m,
            CancellationToken.None);

        var reviewed = await service.ReviewAsync(id!.Value, approved: true, "nour", CancellationToken.None);
        var stored = await store.GetByIdAsync(id.Value, CancellationToken.None);

        reviewed.Success.Should().BeFalse();
        reviewed.ReasonCode.Should().Be("ai.review.spec_unknown_keys");
        stored!.ReviewStatus.Should().Be("pending");
        applicator.Applied.Should().BeFalse();
    }

    [Test]
    public async Task SaveCandidateAsync_Should_Never_Auto_Publish_For_Any_Content_Type()
    {
        var store = new InMemoryGenerationRepository();
        var service = new AiContentCandidateService(store);

        foreach (var entityType in new[]
                 {
                     AiGenerationEntityType.ProductDescription,
                     AiGenerationEntityType.Specification,
                     AiGenerationEntityType.Translation,
                     AiGenerationEntityType.SeoMetadata
                 })
        {
            var id = await service.SaveCandidateAsync(
                entityType,
                1,
                AiFeatureKeys.ImportEnrichment,
                "en",
                "candidate",
                AiFeatureKeys.ImportEnrichment,
                "hash",
                1m,
                CancellationToken.None);

            var stored = await store.GetByIdAsync(id!.Value, CancellationToken.None);
            stored!.IsPublished.Should().BeFalse();
            stored.ReviewStatus.Should().Be("pending");
        }
    }

    [Test]
    public async Task RebindImportRowEntityAsync_Should_Point_Pending_Candidates_To_Product()
    {
        var store = new InMemoryGenerationRepository();
        var service = new AiImportCandidateRebindService(store);

        await store.InsertAsync(new AiGenerationCandidate
        {
            EntityType = AiGenerationEntityType.ProductDescription,
            EntityId = 3,
            FeatureKey = AiFeatureKeys.ImportEnrichment,
            Locale = "en",
            OutputText = "text",
            PromptKey = AiFeatureKeys.ImportEnrichment,
            PromptHash = "hash",
            ReviewStatus = "pending"
        }, CancellationToken.None);

        await service.RebindImportRowAsync(3, 9001, CancellationToken.None);

        var pending = await store.GetPendingByEntityAsync(
            AiGenerationEntityType.ProductDescription,
            9001,
            CancellationToken.None);

        pending.Should().ContainSingle();
    }

    private sealed class InMemoryGenerationRepository : IAiGenerationRepository
    {
        private readonly Dictionary<int, AiGenerationCandidate> _items = new();
        private int _nextId = 1;

        public Task<int> InsertAsync(AiGenerationCandidate candidate, CancellationToken cancellationToken)
        {
            candidate.Id = _nextId++;
            _items[candidate.Id] = candidate;
            return Task.FromResult(candidate.Id);
        }

        public Task<AiGenerationCandidate?> GetByIdAsync(int id, CancellationToken cancellationToken)
        {
            _items.TryGetValue(id, out var candidate);
            return Task.FromResult(candidate);
        }

        public Task<IReadOnlyList<AiGenerationCandidate>> GetPendingByEntityAsync(
            AiGenerationEntityType entityType,
            int entityId,
            CancellationToken cancellationToken)
        {
            return Task.FromResult<IReadOnlyList<AiGenerationCandidate>>(
                _items.Values.Where(x => x.EntityType == entityType && x.EntityId == entityId && x.ReviewStatus == "pending").ToList());
        }

        public Task MarkReviewedAsync(int id, bool approved, string reviewer, CancellationToken cancellationToken)
        {
            if (_items.TryGetValue(id, out var candidate))
            {
                candidate.ReviewStatus = approved ? "approved" : "rejected";
                candidate.IsPublished = approved;
                candidate.Reviewer = reviewer;
            }

            return Task.CompletedTask;
        }

        public Task RebindImportRowEntityAsync(int importRowNumber, int productId, CancellationToken cancellationToken)
        {
            foreach (var candidate in _items.Values.Where(x => x.EntityId == importRowNumber && x.ReviewStatus == "pending"))
                candidate.EntityId = productId;

            return Task.CompletedTask;
        }
    }

    private sealed class RecordingApplicator : IAiContentApplicator
    {
        public bool Applied { get; private set; }

        public AiGenerationCandidate? LastCandidate { get; private set; }

        public Task<AiContentApplyResult> ApplyAsync(AiGenerationCandidate candidate, CancellationToken cancellationToken)
        {
            Applied = true;
            LastCandidate = candidate;
            return Task.FromResult(AiContentApplyResult.Ok());
        }
    }
}
