using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using NUnit.Framework;
using TwinParticles.CheckEngine.Application.Fitment;
using TwinParticles.CheckEngine.Domain.Fitment;

namespace TwinParticles.CheckEngine.Tests.Architecture;

[TestFixture]
public class FitmentPolicyAndReviewServiceTests
{
    [Test]
    public async Task TryPublishAsync_Should_Publish_NonAi_HighConfidence_Standard_Claim()
    {
        var repository = new FakeWriteRepository();
        var service = new FitmentPublicationPolicyService(repository);

        var claim = new FitmentClaim
        {
            Id = 100,
            ProductId = 10,
            VehicleConfigurationId = 20,
            Confidence = 0.92m,
            SafetyClass = SafetyClass.Standard,
            Provenance = new FitmentClaimProvenance { SourceKind = FitmentSourceKind.CuratorManual },
            IsPublished = false,
            IsActive = true
        };

        var published = await service.TryPublishAsync(claim, CancellationToken.None);

        published.Should().BeTrue();
        claim.IsPublished.Should().BeTrue();
        repository.LastUpsertedClaim.Should().NotBeNull();
        repository.PublishedByClaimId[100].Should().BeTrue();
    }

    [Test]
    public async Task TryPublishAsync_Should_Be_Idempotent_When_Claim_Already_Published()
    {
        var repository = new FakeWriteRepository();
        var service = new FitmentPublicationPolicyService(repository);

        var claim = new FitmentClaim
        {
            Id = 101,
            ProductId = 10,
            VehicleConfigurationId = 21,
            Confidence = 0.92m,
            SafetyClass = SafetyClass.Standard,
            Provenance = new FitmentClaimProvenance { SourceKind = FitmentSourceKind.CuratorManual },
            IsPublished = true,
            IsActive = true
        };

        var published = await service.TryPublishAsync(claim, CancellationToken.None);

        published.Should().BeTrue();
        repository.UpsertCount.Should().Be(0);
        repository.SetPublishedCount.Should().Be(0);
    }

    [Test]
    public void TryPublishAsync_Should_Reject_When_Claim_Is_Null()
    {
        var service = new FitmentPublicationPolicyService(new FakeWriteRepository());

        Func<Task> act = async () => await service.TryPublishAsync(null!, CancellationToken.None);

        act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Test]
    public async Task TryPublishAsync_Should_Reject_When_Claim_Id_Is_Invalid()
    {
        var service = new FitmentPublicationPolicyService(new FakeWriteRepository());

        var claim = new FitmentClaim
        {
            Id = 0,
            ProductId = 10,
            VehicleConfigurationId = 20,
            Confidence = 0.92m,
            SafetyClass = SafetyClass.Standard,
            Provenance = new FitmentClaimProvenance { SourceKind = FitmentSourceKind.CuratorManual }
        };

        var published = await service.TryPublishAsync(claim, CancellationToken.None);

        published.Should().BeFalse();
    }

    [Test]
    public async Task TryPublishAsync_Should_Reject_When_Claim_Is_Inactive()
    {
        var service = new FitmentPublicationPolicyService(new FakeWriteRepository());

        var claim = new FitmentClaim
        {
            Id = 102,
            ProductId = 10,
            VehicleConfigurationId = 20,
            Confidence = 0.92m,
            SafetyClass = SafetyClass.Standard,
            Provenance = new FitmentClaimProvenance { SourceKind = FitmentSourceKind.CuratorManual },
            IsActive = false
        };

        var published = await service.TryPublishAsync(claim, CancellationToken.None);

        published.Should().BeFalse();
    }

    [Test]
    public async Task ApproveAsync_Should_Set_Fits_And_Published_And_Reject_Invalid_Id()
    {
        var readRepository = new FakeReadRepository();
        var writeRepository = new FakeWriteRepository();
        var queueRepository = new FakeReviewQueueRepository();
        var service = new FitmentReviewService(readRepository, writeRepository, queueRepository, new NoOpAuditService());

        await service.ApproveAsync(300, CancellationToken.None);

        writeRepository.StatusByClaimId[300].Should().Be(FitmentStatus.Fits);
        writeRepository.PublishedByClaimId[300].Should().BeTrue();

        Func<Task> invalidAct = async () => await service.ApproveAsync(0, CancellationToken.None);
        await invalidAct.Should().ThrowAsync<ArgumentException>();
    }

    [Test]
    public async Task RejectAsync_Should_Set_Rejected_And_Unpublish_Without_Reenqueue()
    {
        var readRepository = new FakeReadRepository();
        var writeRepository = new FakeWriteRepository();
        var queueRepository = new FakeReviewQueueRepository();
        var service = new FitmentReviewService(readRepository, writeRepository, queueRepository, new NoOpAuditService());

        await service.RejectAsync(301, CancellationToken.None);

        writeRepository.StatusByClaimId[301].Should().Be(FitmentStatus.Rejected);
        writeRepository.PublishedByClaimId[301].Should().BeFalse();
        queueRepository.Enqueued.Should().BeEmpty();

        Func<Task> invalidAct = async () => await service.RejectAsync(-1, CancellationToken.None);
        await invalidAct.Should().ThrowAsync<ArgumentException>();
    }

    [Test]
    public async Task ApproveAsync_Should_Preserve_DoesNotFit_Verdict()
    {
        var claim = new FitmentClaim
        {
            Id = 302,
            ProductId = 10,
            VehicleConfigurationId = 20,
            Status = FitmentStatus.DoesNotFit,
            IsPublished = false,
            IsActive = true,
            Provenance = new FitmentClaimProvenance { SourceKind = FitmentSourceKind.AiInference }
        };
        var writeRepository = new FakeWriteRepository();
        var queueRepository = new FakeReviewQueueRepository();
        var service = new FitmentReviewService(
            new FakeReadRepository(claim),
            writeRepository,
            queueRepository,
            new NoOpAuditService());

        await service.ApproveAsync(302, CancellationToken.None);

        writeRepository.StatusByClaimId[302].Should().Be(FitmentStatus.DoesNotFit);
        writeRepository.PublishedByClaimId[302].Should().BeTrue();
        queueRepository.Dequeued.Should().ContainSingle(x => x.claimId == 302 && x.reasonCode == "fitment.review.approved");
    }

    [Test]
    public async Task RejectAsync_Should_Deactivate_Claim()
    {
        var claim = new FitmentClaim
        {
            Id = 304,
            ProductId = 10,
            VehicleConfigurationId = 20,
            Status = FitmentStatus.Unknown,
            IsPublished = false,
            IsActive = true,
            Provenance = new FitmentClaimProvenance { SourceKind = FitmentSourceKind.AiInference }
        };
        var writeRepository = new FakeWriteRepository();
        var queueRepository = new FakeReviewQueueRepository();
        var service = new FitmentReviewService(
            new FakeReadRepository(claim),
            writeRepository,
            queueRepository,
            new NoOpAuditService());

        await service.RejectAsync(304, CancellationToken.None);

        writeRepository.LastUpsertedClaim.Should().NotBeNull();
        writeRepository.LastUpsertedClaim!.IsActive.Should().BeFalse();
        writeRepository.LastUpsertedClaim.Status.Should().Be(FitmentStatus.Rejected);
    }

    [Test]
    public async Task ApproveAsync_Should_Promote_Ai_Source_To_CuratorManual()
    {
        var claim = new FitmentClaim
        {
            Id = 305,
            ProductId = 10,
            VehicleConfigurationId = 20,
            Status = FitmentStatus.Fits,
            IsPublished = false,
            IsActive = true,
            Provenance = new FitmentClaimProvenance
            {
                SourceKind = FitmentSourceKind.AiInference,
                SourceReference = "hash|rationale"
            }
        };
        var writeRepository = new FakeWriteRepository();
        var service = new FitmentReviewService(
            new FakeReadRepository(claim),
            writeRepository,
            new FakeReviewQueueRepository(),
            new NoOpAuditService());

        await service.ApproveAsync(305, CancellationToken.None, "nour");

        writeRepository.LastUpsertedClaim.Should().NotBeNull();
        writeRepository.LastUpsertedClaim!.Provenance.SourceKind.Should().Be(FitmentSourceKind.CuratorManual);
        writeRepository.LastUpsertedClaim.Provenance.SourceReference.Should().Be("ai.review.promoted");
        writeRepository.LastUpsertedClaim.Provenance.CreatedBy.Should().Be("nour");
    }

    [Test]
    public async Task ApproveAsync_Should_Invalidate_Fitment_Cache_For_The_Claim()
    {
        var claim = new FitmentClaim
        {
            Id = 400,
            ProductId = 77,
            VehicleConfigurationId = 88,
            Status = FitmentStatus.Unknown,
            IsActive = true
        };
        var cache = new RecordingFitmentCache();
        var service = new FitmentReviewService(
            new FakeReadRepository(claim),
            new FakeWriteRepository(),
            new FakeReviewQueueRepository(),
            new NoOpAuditService(),
            fitmentCache: cache);

        await service.ApproveAsync(400, CancellationToken.None);

        cache.Invalidations.Should().ContainSingle(x => x.productId == 77 && x.vehicleConfigurationId == 88);
    }

    [Test]
    public async Task GetAiQueueAsync_Should_Exclude_Rejected_Inactive_Or_Published_Ai_Claims()
    {
        var claims = new List<FitmentClaim>
        {
            new()
            {
                Id = 1,
                ProductId = 10,
                VehicleConfigurationId = 20,
                Status = FitmentStatus.Unknown,
                IsPublished = false,
                IsActive = true,
                Provenance = new FitmentClaimProvenance { SourceKind = FitmentSourceKind.AiInference }
            },
            new()
            {
                Id = 2,
                ProductId = 11,
                VehicleConfigurationId = 21,
                Status = FitmentStatus.Rejected,
                IsPublished = false,
                IsActive = false,
                Provenance = new FitmentClaimProvenance { SourceKind = FitmentSourceKind.AiInference }
            },
            new()
            {
                Id = 3,
                ProductId = 12,
                VehicleConfigurationId = 22,
                Status = FitmentStatus.Fits,
                IsPublished = true,
                IsActive = true,
                Provenance = new FitmentClaimProvenance { SourceKind = FitmentSourceKind.AiInference }
            }
        };
        var service = new FitmentReviewService(
            new QueueReadRepository(claims),
            new FakeWriteRepository(),
            new FakeReviewQueueRepository(),
            new NoOpAuditService());

        var queue = await service.GetAiQueueAsync(CancellationToken.None);

        queue.Should().ContainSingle(x => x.Id == 1);
    }

    private sealed class QueueReadRepository : IFitmentClaimReadRepository
    {
        private readonly IReadOnlyList<FitmentClaim> _queue;

        public QueueReadRepository(IReadOnlyList<FitmentClaim> queue) => _queue = queue;

        public Task<IReadOnlyList<FitmentClaim>> GetClaimsAsync(int productId, int vehicleConfigurationId, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<FitmentClaim>>([]);

        public Task<FitmentClaim?> GetByIdAsync(int claimId, CancellationToken cancellationToken) =>
            Task.FromResult(_queue.FirstOrDefault(x => x.Id == claimId));

        public Task<IReadOnlyList<FitmentClaim>> GetReviewQueueAsync(CancellationToken cancellationToken) =>
            Task.FromResult(_queue);
    }

    private sealed class FakeReadRepository : IFitmentClaimReadRepository
    {
        private readonly FitmentClaim? _claim;

        public FakeReadRepository(FitmentClaim? claim = null)
        {
            _claim = claim;
        }

        public Task<IReadOnlyList<FitmentClaim>> GetClaimsAsync(int productId, int vehicleConfigurationId, CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyList<FitmentClaim>>([]);

        public Task<IReadOnlyList<FitmentClaim>> GetReviewQueueAsync(CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyList<FitmentClaim>>([]);

        public Task<FitmentClaim?> GetByIdAsync(int claimId, CancellationToken cancellationToken)
            => Task.FromResult(_claim is not null && _claim.Id == claimId ? _claim : null);
    }

    private sealed class RecordingFitmentCache : IFitmentCache
    {
        public List<(int productId, int vehicleConfigurationId)> Invalidations { get; } = [];

        public Task<FitmentEvaluationResult?> GetAsync(FitmentEvaluationContext context, CancellationToken cancellationToken)
            => Task.FromResult<FitmentEvaluationResult?>(null);

        public Task SetAsync(FitmentEvaluationContext context, FitmentEvaluationResult result, CancellationToken cancellationToken)
            => Task.CompletedTask;

        public Task InvalidateAsync(int productId, int vehicleConfigurationId, CancellationToken cancellationToken)
        {
            Invalidations.Add((productId, vehicleConfigurationId));
            return Task.CompletedTask;
        }
    }

    private sealed class FakeWriteRepository : IFitmentClaimWriteRepository
    {
        public FitmentClaim? LastUpsertedClaim { get; private set; }
        public int UpsertCount { get; private set; }
        public int SetPublishedCount { get; private set; }
        public Dictionary<int, bool> PublishedByClaimId { get; } = new();
        public Dictionary<int, FitmentStatus> StatusByClaimId { get; } = new();

        public Task UpsertAsync(FitmentClaim claim, CancellationToken cancellationToken)
        {
            LastUpsertedClaim = claim;
            UpsertCount++;
            return Task.CompletedTask;
        }

        public Task SetPublishedAsync(int claimId, bool isPublished, CancellationToken cancellationToken)
        {
            SetPublishedCount++;
            PublishedByClaimId[claimId] = isPublished;
            return Task.CompletedTask;
        }

        public Task SetStatusAsync(int claimId, FitmentStatus status, CancellationToken cancellationToken)
        {
            StatusByClaimId[claimId] = status;
            return Task.CompletedTask;
        }
    }

    private sealed class FakeReviewQueueRepository : IFitmentReviewQueueRepository
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
