using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using NUnit.Framework;
using TwinParticles.CheckEngine.Application.Fitment;
using TwinParticles.CheckEngine.Domain.Ai;
using TwinParticles.CheckEngine.Domain.Fitment;

namespace TwinParticles.CheckEngine.Tests.Architecture;

[TestFixture]
public class FitmentInferenceServiceTests
{
    [Test]
    public async Task InferCandidateAsync_Should_Create_Unpublished_Ai_Claim()
    {
        var writeRepo = new InMemoryFitmentWriteRepository();
        var queueRepo = new RecordingQueueRepository();
        var service = new FitmentInferenceService(writeRepo, queueRepo, new FitsPort());

        var claim = await service.InferCandidateAsync(10, 20, "Water Pump", CancellationToken.None);

        claim.Should().NotBeNull();
        claim!.SourceKindIsAi().Should().BeTrue();
        claim.IsPublished.Should().BeFalse();
        claim.Confidence.Should().BeLessThanOrEqualTo(FitmentInferenceService.AiInferenceConfidenceCap);
        queueRepo.LastReason.Should().Be("fitment.ai_inference");
    }

    [Test]
    public async Task InferCandidateAsync_Should_Parse_Json_Verdict_From_Prompt_Store()
    {
        var writeRepo = new InMemoryFitmentWriteRepository();
        var queueRepo = new RecordingQueueRepository();
        var service = new FitmentInferenceService(writeRepo, queueRepo, new JsonVerdictPort());

        var claim = await service.InferCandidateAsync(10, 20, "Brake Pad", CancellationToken.None);

        claim.Should().NotBeNull();
        claim!.Status.Should().Be(FitmentStatus.DoesNotFit);
        FitmentAiReference.Parse(claim.Provenance.SourceReference).Rationale.Should().Be("OEM cross-reference missing");
    }

    private sealed class JsonVerdictPort : IAiCompletionPort
    {
        public Task<AiCompletionResult> CompleteAsync(AiCompletionRequest request, CancellationToken ct)
        {
            return Task.FromResult(new AiCompletionResult
            {
                Success = true,
                Text = """[{"productId":10,"configurationId":20,"verdict":"DoesNotFit","confidence":0.4,"rationale":"OEM cross-reference missing"}]""",
                ProviderName = "test",
                PromptHash = "abc"
            });
        }
    }

    private sealed class FitsPort : IAiCompletionPort
    {
        public Task<AiCompletionResult> CompleteAsync(AiCompletionRequest request, CancellationToken ct)
        {
            return Task.FromResult(new AiCompletionResult
            {
                Success = true,
                Text = "Fits",
                ProviderName = "test",
                PromptHash = "abc"
            });
        }
    }

    private sealed class InMemoryFitmentWriteRepository : IFitmentClaimWriteRepository
    {
        private int _nextId = 1;

        public Task UpsertAsync(FitmentClaim claim, CancellationToken cancellationToken)
        {
            if (claim.Id <= 0)
                claim.Id = _nextId++;

            return Task.CompletedTask;
        }

        public Task SetPublishedAsync(int claimId, bool isPublished, CancellationToken cancellationToken) =>
            Task.CompletedTask;

        public Task SetStatusAsync(int claimId, FitmentStatus status, CancellationToken cancellationToken) =>
            Task.CompletedTask;
    }

    private sealed class RecordingQueueRepository : IFitmentReviewQueueRepository
    {
        public string? LastReason { get; private set; }

        public Task EnqueueAsync(int claimId, string reasonCode, CancellationToken cancellationToken)
        {
            LastReason = reasonCode;
            return Task.CompletedTask;
        }

        public Task DequeueAsync(int claimId, string reasonCode, CancellationToken cancellationToken) =>
            Task.CompletedTask;
    }
}
