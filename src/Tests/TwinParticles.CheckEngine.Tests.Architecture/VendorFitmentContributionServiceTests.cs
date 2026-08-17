using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using NUnit.Framework;
using TwinParticles.CheckEngine.Application.Licensing;
using TwinParticles.CheckEngine.Application.Marketplace;
using TwinParticles.CheckEngine.Domain.Fitment;
using TwinParticles.CheckEngine.Domain.Licensing;
using TwinParticles.CheckEngine.Domain.Marketplace;
using TwinParticles.CheckEngine.Domain.Security;
using TwinParticles.CheckEngine.Infrastructure.Security;

namespace TwinParticles.CheckEngine.Tests.Architecture;

[TestFixture]
public class VendorFitmentContributionServiceTests
{
    [Test]
    public async Task Revoke_Should_Deactivate_Own_Claim_For_Vendor()
    {
        var harness = Harness.Create();
        await harness.Ownership.AssignProductAsync(1, 100, CancellationToken.None);
        var submit = await harness.Service.SubmitProposalAsync(
            VendorActor.Vendor(1),
            new VendorFitmentProposalRequest { ProductId = 100, VehicleConfigurationId = 55 },
            CancellationToken.None);

        var revoke = await harness.Service.RevokeProposalAsync(VendorActor.Vendor(1), submit.ClaimId!.Value, CancellationToken.None);

        revoke.Succeeded.Should().BeTrue();
        var claim = harness.Claims.Claims.Single();
        claim.IsActive.Should().BeFalse();
        claim.IsPublished.Should().BeFalse();
        claim.ValidToUtc.Should().NotBeNull();
    }

    [Test]
    public async Task Revoke_Should_Deny_Cross_Vendor_Claim()
    {
        var harness = Harness.Create();
        await harness.Ownership.AssignProductAsync(1, 100, CancellationToken.None);
        var submit = await harness.Service.SubmitProposalAsync(
            VendorActor.Vendor(1),
            new VendorFitmentProposalRequest { ProductId = 100, VehicleConfigurationId = 55 },
            CancellationToken.None);

        var revoke = await harness.Service.RevokeProposalAsync(VendorActor.Vendor(2), submit.ClaimId!.Value, CancellationToken.None);

        revoke.Succeeded.Should().BeFalse();
        revoke.ReasonCode.Should().Be(VendorErrorCodes.IsolationDenied);
    }

    [Test]
    public async Task Revoke_Should_Allow_Operator_For_Any_Vendor_Claim()
    {
        var harness = Harness.Create();
        await harness.Ownership.AssignProductAsync(1, 100, CancellationToken.None);
        var submit = await harness.Service.SubmitProposalAsync(
            VendorActor.Vendor(1),
            new VendorFitmentProposalRequest { ProductId = 100, VehicleConfigurationId = 55 },
            CancellationToken.None);

        var revoke = await harness.Service.RevokeProposalAsync(VendorActor.OperatorAdmin, submit.ClaimId!.Value, CancellationToken.None);

        revoke.Succeeded.Should().BeTrue();
    }

    [Test]
    public async Task Revoke_Should_Deny_Operator_Claims_Without_Vendor_Attribution()
    {
        var harness = Harness.Create();
        await harness.Claims.UpsertAsync(new FitmentClaim
        {
            Id = 1,
            ProductId = 100,
            VehicleConfigurationId = 55,
            IsActive = true,
            Provenance = new FitmentClaimProvenance
            {
                SourceKind = FitmentSourceKind.CuratorManual,
                SourceReference = "operator.manual",
                CreatedBy = "admin"
            }
        }, CancellationToken.None);

        var revoke = await harness.Service.RevokeProposalAsync(VendorActor.OperatorAdmin, 1, CancellationToken.None);

        revoke.Succeeded.Should().BeFalse();
        revoke.ReasonCode.Should().Be("vendor.fitment.not_vendor_claim");
    }

    private sealed class Harness
    {
        public required VendorFitmentContributionService Service { get; init; }
        public required InMemoryOwnership Ownership { get; init; }
        public required InMemoryFitmentClaims Claims { get; init; }

        public static Harness Create()
        {
            var ownership = new InMemoryOwnership();
            var claims = new InMemoryFitmentClaims();
            var queue = new RecordingFitmentQueue();
            var audit = new InMemoryCheckEngineAuditService();
            var licenceGate = new MarketplaceLicenceGate(new StubLicenceService());
            var isolation = new VendorIsolationService(ownership, new EmptyOrders(), licenceGate, audit);
            var service = new VendorFitmentContributionService(isolation, claims, claims, queue, licenceGate, audit);

            return new Harness { Service = service, Ownership = ownership, Claims = claims };
        }
    }

    private sealed class EmptyOrders : IVendorOrderReadStore
    {
        public Task<IReadOnlyList<VendorOrderRecord>> GetOrdersForProductsAsync(IReadOnlyCollection<int> productIds, CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyList<VendorOrderRecord>>([]);
    }

    private sealed class InMemoryOwnership : IVendorOwnershipStore
    {
        private readonly Dictionary<int, int> _map = [];

        public Task AssignProductAsync(int vendorId, int productId, CancellationToken cancellationToken)
        {
            _map[productId] = vendorId;
            return Task.CompletedTask;
        }

        public Task<int?> GetProductVendorIdAsync(int productId, CancellationToken cancellationToken)
            => Task.FromResult(_map.TryGetValue(productId, out var vendorId) ? (int?)vendorId : null);

        public Task<IReadOnlyList<int>> GetProductIdsAsync(int vendorId, CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyList<int>>(_map.Where(pair => pair.Value == vendorId).Select(pair => pair.Key).ToList());

        public Task<IReadOnlyList<int>> GetAllMappedProductIdsAsync(CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyList<int>>(_map.Keys.ToList());

        public Task<int> AssignUnmappedProductsAsync(int operatorVendorId, IReadOnlyList<int> productIds, CancellationToken cancellationToken)
            => Task.FromResult(0);
    }

    private sealed class InMemoryFitmentClaims : IFitmentClaimReadRepository, IFitmentClaimWriteRepository
    {
        private readonly List<FitmentClaim> _claims = [];
        private int _nextId = 1;

        public IReadOnlyList<FitmentClaim> Claims => _claims;

        public Task<IReadOnlyList<FitmentClaim>> GetClaimsAsync(int productId, int vehicleConfigurationId, CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyList<FitmentClaim>>([]);

        public Task<IReadOnlyList<FitmentClaim>> GetReviewQueueAsync(CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyList<FitmentClaim>>([]);

        public Task<IReadOnlyList<FitmentClaim>> GetAllClaimsAsync(CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyList<FitmentClaim>>(_claims.ToList());

        public Task<IReadOnlyList<FitmentClaim>> GetClaimsByVendorIdAsync(int vendorId, CancellationToken cancellationToken)
        {
            var prefix = VendorSourceReference.ForVendor(vendorId);
            return Task.FromResult<IReadOnlyList<FitmentClaim>>(_claims
                .Where(c => c.VendorId == vendorId || c.Provenance.SourceReference == prefix)
                .ToList());
        }

        public Task<FitmentClaim?> GetByIdAsync(int claimId, CancellationToken cancellationToken)
            => Task.FromResult(_claims.FirstOrDefault(c => c.Id == claimId));

        public Task UpsertAsync(FitmentClaim claim, CancellationToken cancellationToken)
        {
            if (claim.Id == 0)
                claim.Id = _nextId++;
            _claims.RemoveAll(c => c.Id == claim.Id);
            _claims.Add(claim);
            return Task.CompletedTask;
        }

        public Task SetPublishedAsync(int claimId, bool isPublished, CancellationToken cancellationToken)
        {
            var claim = _claims.FirstOrDefault(c => c.Id == claimId);
            if (claim is not null)
                claim.IsPublished = isPublished;
            return Task.CompletedTask;
        }

        public Task SetStatusAsync(int claimId, FitmentStatus status, CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class RecordingFitmentQueue : IFitmentReviewQueueRepository
    {
        public ConcurrentBag<int> Enqueued { get; } = [];

        public Task EnqueueAsync(int claimId, string reasonCode, CancellationToken cancellationToken)
        {
            Enqueued.Add(claimId);
            return Task.CompletedTask;
        }

        public Task DequeueAsync(int claimId, string reasonCode, CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class StubLicenceService : ILicenceService
    {
        public Task<LicenceStatus> GetStatusAsync(CancellationToken cancellationToken)
            => Task.FromResult(new LicenceStatus
            {
                IsActive = true,
                MarketplaceModuleEntitlement = true,
                Tier = LicenceTier.Business
            });

        public Task<LicenceStatus> ActivateAsync(string licenceKey, CancellationToken cancellationToken)
            => GetStatusAsync(cancellationToken);

        public Task<LicenceStatus> HeartbeatAsync(CancellationToken cancellationToken)
            => GetStatusAsync(cancellationToken);
    }
}
