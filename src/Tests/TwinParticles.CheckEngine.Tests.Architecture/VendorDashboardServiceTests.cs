using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using NUnit.Framework;
using TwinParticles.CheckEngine.Application.Erp;
using TwinParticles.CheckEngine.Domain.Erp;
using TwinParticles.CheckEngine.Application.Licensing;
using TwinParticles.CheckEngine.Application.Marketplace;
using TwinParticles.CheckEngine.Domain.Fitment;
using TwinParticles.CheckEngine.Domain.Licensing;
using TwinParticles.CheckEngine.Domain.Marketplace;
using TwinParticles.CheckEngine.Infrastructure.Security;

namespace TwinParticles.CheckEngine.Tests.Architecture;

[TestFixture]
public class VendorDashboardServiceTests
{
    [Test]
    public async Task Vendor_Should_Update_Own_Inventory_Only()
    {
        var harness = Harness.Create();
        await harness.Ownership.AssignProductAsync(1, 100, CancellationToken.None);
        await harness.Ownership.AssignProductAsync(2, 200, CancellationToken.None);

        var allowed = await harness.Service.UpdateInventoryAsync(VendorActor.Vendor(1), 100, 42, CancellationToken.None);
        var denied = await harness.Service.UpdateInventoryAsync(VendorActor.Vendor(1), 200, 10, CancellationToken.None);

        allowed.Succeeded.Should().BeTrue();
        denied.Succeeded.Should().BeFalse();
        denied.ReasonCode.Should().Be(VendorErrorCodes.IsolationDenied);
        harness.Inventory.GetStock(100).Should().Be(42);
    }

    [Test]
    public async Task Dashboard_Should_Aggregate_Catalog_Inventory_And_Orders()
    {
        var harness = Harness.Create();
        await harness.Ownership.AssignProductAsync(1, 100, CancellationToken.None);
        await harness.Ownership.AssignProductAsync(1, 101, CancellationToken.None);
        harness.Inventory.Set(100, "Brake pad", 3);
        harness.Inventory.Set(101, "Filter", 20);
        harness.Orders.Add(new VendorOrderRecord { OrderId = 10, CustomerId = 501, ProductIds = [100] });
        harness.Orders.Add(new VendorOrderRecord { OrderId = 11, CustomerId = 502, ProductIds = [101] });

        var snapshot = await harness.Service.GetDashboardAsync(VendorActor.Vendor(1), CancellationToken.None);

        snapshot.Should().NotBeNull();
        snapshot!.ProductCount.Should().Be(2);
        snapshot.TotalStockUnits.Should().Be(23);
        snapshot.LowStockCount.Should().Be(1);
        snapshot.OrderCount.Should().Be(2);
        snapshot.CustomerCount.Should().Be(2);
        snapshot.Statements.Available.Should().BeFalse();
        snapshot.Statements.MessageKey.Should().Be("Plugins.TwinParticles.CheckEngine.Marketplace.Statements.None");
    }

    [Test]
    public async Task Scorecard_Should_Compute_Rates_From_Analytics()
    {
        var harness = Harness.Create();
        await harness.Ownership.AssignProductAsync(1, 100, CancellationToken.None);
        harness.Analytics.SetScorecard(new VendorScorecard
        {
            VendorId = 1,
            FillRate = 0.8m,
            CancelRate = 0.1m,
            ClaimRejectionRate = 0.05m,
            OnTimeShipmentRate = 0.9m,
            OrderSampleSize = 10
        });

        var scorecard = await harness.Service.GetScorecardAsync(VendorActor.Vendor(1), CancellationToken.None);

        scorecard!.FillRate.Should().Be(0.8m);
        scorecard.CancelRate.Should().Be(0.1m);
        scorecard.OnTimeShipmentRate.Should().Be(0.9m);
    }

    [Test]
    public async Task SubmitFitmentProposal_Should_Attribute_Vendor_And_Enqueue()
    {
        var harness = Harness.Create();
        await harness.Ownership.AssignProductAsync(1, 100, CancellationToken.None);

        var result = await harness.Service.SubmitFitmentProposalAsync(
            VendorActor.Vendor(1),
            new VendorFitmentProposalRequest { ProductId = 100, VehicleConfigurationId = 55 },
            CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        result.ClaimId.Should().BeGreaterThan(0);
        harness.FitmentClaims.Claims.Single().Provenance.SourceReference.Should().Be(VendorSourceReference.ForVendor(1));
        harness.FitmentQueue.Enqueued.Should().Contain(result.ClaimId!.Value);
    }

    [Test]
    public void VendorSourceReference_Should_Parse_Vendor_Id()
    {
        VendorSourceReference.ForVendor(7).Should().Be("vendor:7");
        VendorSourceReference.TryParseVendorId("vendor:7", out var id).Should().BeTrue();
        id.Should().Be(7);
        VendorSourceReference.TryParseVendorId("other", out _).Should().BeFalse();
    }

    private sealed class Harness
    {
        public required VendorDashboardService Service { get; init; }
        public required InMemoryOwnership Ownership { get; init; }
        public required InMemoryOrders Orders { get; init; }
        public required InMemoryInventory Inventory { get; init; }
        public required StubAnalytics Analytics { get; init; }
        public required InMemoryFitmentClaims FitmentClaims { get; init; }
        public required RecordingFitmentQueue FitmentQueue { get; init; }

        public static Harness Create()
        {
            var ownership = new InMemoryOwnership();
            var orders = new InMemoryOrders();
            var inventory = new InMemoryInventory();
            var analytics = new StubAnalytics();
            var fitmentClaims = new InMemoryFitmentClaims();
            var fitmentQueue = new RecordingFitmentQueue();
            var audit = new InMemoryCheckEngineAuditService();
            var licenceGate = new MarketplaceLicenceGate(new StubLicenceService());
            var isolation = new VendorIsolationService(ownership, orders, licenceGate, audit);
            var vendors = new StubVendors();
            var payout = new PayoutStatementService(
                new PayoutStatementBuilder(),
                new InMemoryPayoutRepository(),
                new EmptyPayoutDataSource(),
                new ErpSyncService(new EmptyErpQueue(), new StubErpClient(), new NoResolveErp()),
                licenceGate,
                audit);

            var service = new VendorDashboardService(
                isolation,
                inventory,
                analytics,
                vendors,
                fitmentClaims,
                fitmentClaims,
                fitmentQueue,
                payout,
                licenceGate,
                audit);

            return new Harness
            {
                Service = service,
                Ownership = ownership,
                Orders = orders,
                Inventory = inventory,
                Analytics = analytics,
                FitmentClaims = fitmentClaims,
                FitmentQueue = fitmentQueue
            };
        }
    }

    private sealed class StubVendors : IVendorRepository
    {
        public Task<int> InsertAsync(Vendor vendor, CancellationToken cancellationToken) => Task.FromResult(1);
        public Task UpdateAsync(Vendor vendor, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task<Vendor?> GetByIdAsync(int id, CancellationToken cancellationToken)
            => Task.FromResult<Vendor?>(new Vendor { Id = id, LegalName = $"Vendor {id}", Status = VendorStatus.Active });
        public Task<IReadOnlyList<Vendor>> GetByStatusesAsync(IReadOnlyCollection<VendorStatus> statuses, CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyList<Vendor>>([]);
        public Task<Vendor?> GetOperatorAsync(CancellationToken cancellationToken) => Task.FromResult<Vendor?>(null);
        public Task<Vendor?> GetByApplicantCustomerIdAsync(int customerId, CancellationToken cancellationToken) => Task.FromResult<Vendor?>(null);
        public Task InsertAgreementAsync(VendorAgreementAcceptance acceptance, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task<VendorAgreementAcceptance?> GetLatestAgreementAsync(int vendorId, CancellationToken cancellationToken) => Task.FromResult<VendorAgreementAcceptance?>(null);
        public Task<bool> HasAcceptedAgreementAsync(int vendorId, string agreementVersion, CancellationToken cancellationToken) => Task.FromResult(true);
    }

    private sealed class InMemoryOwnership : IVendorOwnershipStore
    {
        private readonly Dictionary<int, int> _map = new();

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

    private sealed class InMemoryPayoutRepository : IPayoutStatementRepository
    {
        public Task<int> InsertAsync(PayoutStatement statement, CancellationToken cancellationToken) => Task.FromResult(1);
        public Task UpdateAsync(PayoutStatement statement, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task<PayoutStatement?> GetByIdAsync(int statementId, CancellationToken cancellationToken) => Task.FromResult<PayoutStatement?>(null);
        public Task<IReadOnlyList<PayoutStatement>> ListByVendorAsync(int vendorId, CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<PayoutStatement>>([]);
        public Task<PayoutStatement?> GetLatestByVendorAsync(int vendorId, CancellationToken cancellationToken) => Task.FromResult<PayoutStatement?>(null);
        public Task InsertAdjustmentAsync(PayoutAdjustment adjustment, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task<IReadOnlyList<PayoutAdjustment>> GetPendingAdjustmentsAsync(int vendorId, CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<PayoutAdjustment>>([]);
        public Task AssignAdjustmentsToStatementAsync(int vendorId, int statementId, CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class EmptyPayoutDataSource : IPayoutStatementDataSource
    {
        public Task<IReadOnlyList<PayoutSourceLine>> GetVendorLinesAsync(int vendorId, DateTime periodStartUtc, DateTime periodEndUtc, CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyList<PayoutSourceLine>>([]);
    }

    private sealed class EmptyErpQueue : IErpSyncQueueRepository
    {
        public Task<Guid> EnqueueAsync(ErpSyncJob job, CancellationToken cancellationToken) => Task.FromResult(job.JobId);
        public Task<IReadOnlyList<ErpSyncJob>> GetPendingAsync(CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<ErpSyncJob>>([]);
        public Task<ErpSyncJob?> GetByIdAsync(Guid jobId, CancellationToken cancellationToken) => Task.FromResult<ErpSyncJob?>(null);
        public Task UpdateAsync(ErpSyncJob job, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task<IReadOnlyList<ErpSyncJob>> GetAllAsync(CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<ErpSyncJob>>([]);
    }

    private sealed class StubErpClient : IErpClientAdapter
    {
        public Task<bool> PushAsync(ErpSyncJob job, CancellationToken cancellationToken) => Task.FromResult(true);
        public Task<bool> PullAsync(ErpSyncJob job, CancellationToken cancellationToken) => Task.FromResult(true);
        public Task<string?> PullInventorySnapshotAsync(CancellationToken cancellationToken) => Task.FromResult<string?>(null);
    }

    private sealed class NoResolveErp : IErpConflictResolutionService
    {
        public bool CanAutoResolve(ErpSyncJob job) => false;
        public void ApplyAutoResolution(ErpSyncJob job) { }
    }

    private sealed class InMemoryOrders : IVendorOrderReadStore
    {
        private readonly List<VendorOrderRecord> _orders = [];

        public void Add(VendorOrderRecord record) => _orders.Add(record);

        public Task<IReadOnlyList<VendorOrderRecord>> GetOrdersForProductsAsync(
            IReadOnlyCollection<int> productIds,
            CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyList<VendorOrderRecord>>(_orders.Where(o => o.ProductIds.Any(productIds.Contains)).ToList());
    }

    private sealed class InMemoryInventory : IVendorInventoryStore
    {
        private readonly Dictionary<int, VendorInventoryItem> _items = new();

        public void Set(int productId, string name, int stock)
            => _items[productId] = new VendorInventoryItem { ProductId = productId, Name = name, StockQuantity = stock, Published = true };

        public int GetStock(int productId) => _items.TryGetValue(productId, out var item) ? item.StockQuantity : -1;

        public Task<IReadOnlyList<VendorInventoryItem>> ListAsync(IReadOnlyCollection<int> productIds, CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyList<VendorInventoryItem>>(productIds.Where(_items.ContainsKey).Select(id => _items[id]).ToList());

        public Task UpdateStockAsync(int productId, int stockQuantity, CancellationToken cancellationToken)
        {
            if (_items.TryGetValue(productId, out var item))
                _items[productId] = new VendorInventoryItem { ProductId = productId, Name = item.Name, StockQuantity = stockQuantity, Published = item.Published };
            else
                _items[productId] = new VendorInventoryItem { ProductId = productId, StockQuantity = stockQuantity, Published = true };
            return Task.CompletedTask;
        }
    }

    private sealed class StubAnalytics : IVendorAnalyticsStore
    {
        private VendorScorecard _scorecard = VendorScorecard.Empty();

        public void SetScorecard(VendorScorecard scorecard) => _scorecard = scorecard;

        public Task<VendorScorecard> GetScorecardAsync(
            int vendorId,
            string? vendorName,
            IReadOnlyCollection<int> productIds,
            CancellationToken cancellationToken)
            => Task.FromResult(new VendorScorecard
            {
                VendorId = vendorId,
                VendorName = vendorName,
                FillRate = _scorecard.FillRate,
                CancelRate = _scorecard.CancelRate,
                ClaimRejectionRate = _scorecard.ClaimRejectionRate,
                OnTimeShipmentRate = _scorecard.OnTimeShipmentRate,
                OrderSampleSize = _scorecard.OrderSampleSize,
                FitmentClaimSampleSize = _scorecard.FitmentClaimSampleSize,
                ShipmentSampleSize = _scorecard.ShipmentSampleSize
            });

        public Task<VendorFitmentProposalSummary> GetFitmentProposalSummaryAsync(int vendorId, CancellationToken cancellationToken)
            => Task.FromResult(new VendorFitmentProposalSummary());
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

        public Task SetPublishedAsync(int claimId, bool isPublished, CancellationToken cancellationToken) => Task.CompletedTask;
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
                State = "active",
                AllowsAdminWrite = true,
                MarketplaceModuleEntitlement = true,
                Tier = LicenceTier.Business
            });

        public Task<LicenceStatus> ActivateAsync(string licenceKey, CancellationToken cancellationToken)
            => GetStatusAsync(cancellationToken);

        public Task<LicenceStatus> HeartbeatAsync(CancellationToken cancellationToken)
            => GetStatusAsync(cancellationToken);
    }
}
