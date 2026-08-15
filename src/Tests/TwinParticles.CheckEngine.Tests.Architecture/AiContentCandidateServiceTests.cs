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
    public async Task ReviewAsync_Should_Approve_Pending_Candidate()
    {
        var store = new InMemoryGenerationRepository();
        var service = new AiContentCandidateService(store);

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

        reviewed.Should().BeTrue();
        stored!.ReviewStatus.Should().Be("approved");
        stored.IsPublished.Should().BeTrue();
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
    }
}
