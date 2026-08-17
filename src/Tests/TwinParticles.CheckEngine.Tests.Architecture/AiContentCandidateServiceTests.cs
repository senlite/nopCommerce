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

    [Test]
    public async Task ReviewAsync_Should_Return_NotFound_For_Missing_Or_Invalid_Id()
    {
        var empty = new AiContentCandidateService();
        (await empty.ReviewAsync(1, true, "nour", CancellationToken.None)).ReasonCode.Should().Be("ai.review.not_found");

        var store = new InMemoryGenerationRepository();
        var service = new AiContentCandidateService(store);
        (await service.ReviewAsync(0, true, "nour", CancellationToken.None)).ReasonCode.Should().Be("ai.review.not_found");
        (await service.ReviewAsync(99, true, "nour", CancellationToken.None)).ReasonCode.Should().Be("ai.review.not_found");
    }

    [Test]
    public async Task ReviewAsync_Should_Reject_Without_Applying()
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

        var reviewed = await service.ReviewAsync(id!.Value, approved: false, "nour", CancellationToken.None);
        var stored = await store.GetByIdAsync(id.Value, CancellationToken.None);

        reviewed.Success.Should().BeTrue();
        stored!.ReviewStatus.Should().Be("rejected");
        stored.IsPublished.Should().BeFalse();
        applicator.Applied.Should().BeFalse();
    }

    [Test]
    public async Task ReviewAsync_Should_Keep_Pending_When_Apply_Fails()
    {
        var store = new InMemoryGenerationRepository();
        var applicator = new FailingApplicator();
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

        reviewed.Success.Should().BeFalse();
        reviewed.ReasonCode.Should().Be("ai.apply.product_not_found");
        stored!.ReviewStatus.Should().Be("pending");
        stored.IsPublished.Should().BeFalse();
    }

    [Test]
    public async Task GetPendingQueueAsync_Should_Return_Pending_Only()
    {
        var store = new InMemoryGenerationRepository();
        var service = new AiContentCandidateService(store);

        await service.SaveCandidateAsync(
            AiGenerationEntityType.SeoMetadata, 1, AiFeatureKeys.ImportSeo, "en", "seo",
            AiFeatureKeys.ImportSeo, "h1", 1m, CancellationToken.None);
        var approvedId = await service.SaveCandidateAsync(
            AiGenerationEntityType.ProductDescription, 2, AiFeatureKeys.ImportEnrichment, "en", "desc",
            AiFeatureKeys.ImportEnrichment, "h2", 1m, CancellationToken.None);
        await service.ReviewAsync(approvedId!.Value, approved: true, "nour", CancellationToken.None);

        var queue = await service.GetPendingQueueAsync(10, CancellationToken.None);
        queue.Should().ContainSingle(x => x.EntityType == AiGenerationEntityType.SeoMetadata);
        queue.Should().NotContain(x => x.ReviewStatus != "pending");
    }

    [Test]
    public async Task SaveCandidateAsync_Should_Ignore_Blank_Output()
    {
        var store = new InMemoryGenerationRepository();
        var service = new AiContentCandidateService(store);

        var id = await service.SaveCandidateAsync(
            AiGenerationEntityType.Translation, 1, AiFeatureKeys.ImportTranslation, "ar", "  ",
            AiFeatureKeys.ImportTranslation, "h", 1m, CancellationToken.None);

        id.Should().BeNull();
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

        public Task<IReadOnlyList<AiGenerationCandidate>> GetPendingQueueAsync(int take, CancellationToken cancellationToken)
        {
            return Task.FromResult<IReadOnlyList<AiGenerationCandidate>>(
                _items.Values.Where(x => x.ReviewStatus == "pending").Take(Math.Max(1, take)).ToList());
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

    private sealed class FailingApplicator : IAiContentApplicator
    {
        public Task<AiContentApplyResult> ApplyAsync(AiGenerationCandidate candidate, CancellationToken cancellationToken) =>
            Task.FromResult(AiContentApplyResult.Fail("ai.apply.product_not_found"));
    }
}
