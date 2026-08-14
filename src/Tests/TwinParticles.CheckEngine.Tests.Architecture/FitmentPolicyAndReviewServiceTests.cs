using System;
using System.Collections.Generic;
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
    public async Task RejectAsync_Should_Set_Rejected_Unpublish_And_Enqueue_Reason_And_Reject_Invalid_Id()
    {
        var readRepository = new FakeReadRepository();
        var writeRepository = new FakeWriteRepository();
        var queueRepository = new FakeReviewQueueRepository();
        var service = new FitmentReviewService(readRepository, writeRepository, queueRepository, new NoOpAuditService());

        await service.RejectAsync(301, CancellationToken.None);

        writeRepository.StatusByClaimId[301].Should().Be(FitmentStatus.Rejected);
        writeRepository.PublishedByClaimId[301].Should().BeFalse();
        queueRepository.Enqueued.Should().ContainSingle(x => x.claimId == 301 && x.reasonCode == "fitment.rejected_by_reviewer");

        Func<Task> invalidAct = async () => await service.RejectAsync(-1, CancellationToken.None);
        await invalidAct.Should().ThrowAsync<ArgumentException>();
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

        public Task EnqueueAsync(int claimId, string reasonCode, CancellationToken cancellationToken)
        {
            Enqueued.Add((claimId, reasonCode));
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
