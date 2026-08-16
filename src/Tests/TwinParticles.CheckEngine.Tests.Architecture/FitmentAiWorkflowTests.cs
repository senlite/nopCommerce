using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using NUnit.Framework;
using TwinParticles.CheckEngine.Application.Fitment;
using TwinParticles.CheckEngine.Domain.Ai;
using TwinParticles.CheckEngine.Domain.Fitment;

namespace TwinParticles.CheckEngine.Tests.Architecture;

/// <summary>
/// FR-530/531 end-to-end: AI fitment inference creates review candidates; approve/reject dequeue with correct side effects.
/// </summary>
[TestFixture]
public class FitmentAiWorkflowTests
{
    [Test]
    public async Task Infer_Then_Approve_Should_Promote_Source_And_Dequeue()
    {
        var writeRepo = new WorkflowWriteRepository();
        var readRepo = new WorkflowReadRepository(writeRepo);
        var queueRepo = new WorkflowQueueRepository();
        var inference = new FitmentInferenceService(writeRepo, queueRepo, new JsonVerdictPort());
        var review = new FitmentReviewService(readRepo, writeRepo, queueRepo, new NoOpAuditService());

        var claim = await inference.InferCandidateAsync(10, 20, "Brake Pad", CancellationToken.None);
        claim.Should().NotBeNull();

        var queue = await review.GetAiQueueAsync(CancellationToken.None);
        queue.Should().ContainSingle(x => x.Id == claim!.Id);

        await review.ApproveAsync(claim!.Id, CancellationToken.None, "nour");

        writeRepo.Claims[claim.Id].IsPublished.Should().BeTrue();
        writeRepo.Claims[claim.Id].Provenance.SourceKind.Should().Be(FitmentSourceKind.CuratorManual);
        writeRepo.Claims[claim.Id].Provenance.SourceReference.Should().Be("ai.review.promoted");
        queueRepo.Dequeued.Should().ContainSingle(x => x.claimId == claim.Id && x.reasonCode == "fitment.review.approved");

        (await review.GetAiQueueAsync(CancellationToken.None)).Should().BeEmpty();
    }

    [Test]
    public async Task Infer_Then_Reject_Should_Deactivate_And_Dequeue()
    {
        var writeRepo = new WorkflowWriteRepository();
        var readRepo = new WorkflowReadRepository(writeRepo);
        var queueRepo = new WorkflowQueueRepository();
        var inference = new FitmentInferenceService(writeRepo, queueRepo, new FitsPort());
        var review = new FitmentReviewService(readRepo, writeRepo, queueRepo, new NoOpAuditService());

        var claim = await inference.InferCandidateAsync(11, 21, "Water Pump", CancellationToken.None);
        claim.Should().NotBeNull();

        await review.RejectAsync(claim!.Id, CancellationToken.None);

        writeRepo.Claims[claim.Id].IsActive.Should().BeFalse();
        writeRepo.Claims[claim.Id].Status.Should().Be(FitmentStatus.Rejected);
        writeRepo.Claims[claim.Id].IsPublished.Should().BeFalse();
        queueRepo.Dequeued.Should().ContainSingle(x => x.claimId == claim.Id && x.reasonCode == "fitment.review.rejected");

        (await review.GetAiQueueAsync(CancellationToken.None)).Should().BeEmpty();
    }

    [Test]
    public async Task InferCandidateAsync_Should_Cap_Confidence_From_Structured_Verdict()
    {
        var writeRepo = new WorkflowWriteRepository();
        var service = new FitmentInferenceService(writeRepo, new WorkflowQueueRepository(), new HighConfidenceJsonPort());

        var claim = await service.InferCandidateAsync(12, 22, "Filter", CancellationToken.None);

        claim!.Confidence.Should().Be(FitmentInferenceService.AiInferenceConfidenceCap);
    }

    private sealed class JsonVerdictPort : IAiCompletionPort
    {
        public Task<AiCompletionResult> CompleteAsync(AiCompletionRequest request, CancellationToken ct) =>
            Task.FromResult(new AiCompletionResult
            {
                Success = true,
                Text = """{"verdict":"Fits","confidence":0.42,"rationale":"Catalog match"}""",
                PromptHash = "hash-1"
            });
    }

    private sealed class FitsPort : IAiCompletionPort
    {
        public Task<AiCompletionResult> CompleteAsync(AiCompletionRequest request, CancellationToken ct) =>
            Task.FromResult(new AiCompletionResult
            {
                Success = true,
                Text = "Fits",
                PromptHash = "hash-2"
            });
    }

    private sealed class HighConfidenceJsonPort : IAiCompletionPort
    {
        public Task<AiCompletionResult> CompleteAsync(AiCompletionRequest request, CancellationToken ct) =>
            Task.FromResult(new AiCompletionResult
            {
                Success = true,
                Text = """{"verdict":"Fits","confidence":0.95,"rationale":"Too confident"}""",
                PromptHash = "hash-3"
            });
    }

    private sealed class WorkflowWriteRepository : IFitmentClaimWriteRepository
    {
        public Dictionary<int, FitmentClaim> Claims { get; } = new();
        private int _nextId = 1;

        public Task UpsertAsync(FitmentClaim claim, CancellationToken cancellationToken)
        {
            if (claim.Id <= 0)
                claim.Id = _nextId++;

            Claims[claim.Id] = claim;
            return Task.CompletedTask;
        }

        public Task SetPublishedAsync(int claimId, bool isPublished, CancellationToken cancellationToken)
        {
            if (Claims.TryGetValue(claimId, out var claim))
                claim.IsPublished = isPublished;

            return Task.CompletedTask;
        }

        public Task SetStatusAsync(int claimId, FitmentStatus status, CancellationToken cancellationToken)
        {
            if (Claims.TryGetValue(claimId, out var claim))
                claim.Status = status;

            return Task.CompletedTask;
        }
    }

    private sealed class WorkflowReadRepository : IFitmentClaimReadRepository
    {
        private readonly WorkflowWriteRepository _writeRepository;

        public WorkflowReadRepository(WorkflowWriteRepository writeRepository) => _writeRepository = writeRepository;

        public Task<IReadOnlyList<FitmentClaim>> GetClaimsAsync(int productId, int vehicleConfigurationId, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<FitmentClaim>>([]);

        public Task<FitmentClaim?> GetByIdAsync(int claimId, CancellationToken cancellationToken)
        {
            _writeRepository.Claims.TryGetValue(claimId, out var claim);
            return Task.FromResult(claim);
        }

        public Task<IReadOnlyList<FitmentClaim>> GetReviewQueueAsync(CancellationToken cancellationToken)
        {
            var queue = _writeRepository.Claims.Values
                .Where(claim => claim.IsActive && !claim.IsPublished && claim.Status != FitmentStatus.Rejected)
                .ToList();

            return Task.FromResult<IReadOnlyList<FitmentClaim>>(queue);
        }
    }

    private sealed class WorkflowQueueRepository : IFitmentReviewQueueRepository
    {
        public List<(int claimId, string reasonCode)> Enqueued { get; } = [];
        public List<(int claimId, string reasonCode)> Dequeued { get; } = [];

        public Task EnqueueAsync(int claimId, string reasonCode, CancellationToken cancellationToken)
        {
            Enqueued.Add((claimId, reasonCode));
            return Task.CompletedTask;
        }

        public Task DequeueAsync(int claimId, string reasonCode, CancellationToken cancellationToken)
        {
            Dequeued.Add((claimId, reasonCode));
            return Task.CompletedTask;
        }
    }

    private sealed class NoOpAuditService : TwinParticles.CheckEngine.Domain.Security.ICheckEngineAuditService
    {
        public Task AppendAsync(
            string actor,
            string action,
            string entityType,
            string entityId,
            string? beforeJson,
            string? afterJson,
            CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }
}
