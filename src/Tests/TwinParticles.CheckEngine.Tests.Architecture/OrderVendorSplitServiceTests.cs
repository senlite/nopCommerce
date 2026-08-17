using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using NUnit.Framework;
using TwinParticles.CheckEngine.Application.Licensing;
using TwinParticles.CheckEngine.Application.Marketplace;
using TwinParticles.CheckEngine.Domain.Licensing;
using TwinParticles.CheckEngine.Domain.Marketplace;

namespace TwinParticles.CheckEngine.Tests.Architecture;

[TestFixture]
public class OrderVendorSplitServiceTests
{
    [Test]
    public async Task SplitOrder_Should_Create_One_Split_Per_Vendor()
    {
        var store = new InMemorySplitStore();
        var service = CreateService(store, entitled: true, operatorVendorId: 99);

        store.Assign(101, 1);
        store.Assign(102, 2);

        var group = await service.SplitOrderAsync(500, DateTime.UtcNow, [
            new VendorOrderLine { OrderItemId = 1, ProductId = 101, Quantity = 1, PriceExclTax = 10m },
            new VendorOrderLine { OrderItemId = 2, ProductId = 102, Quantity = 2, PriceExclTax = 20m }
        ], CancellationToken.None);

        group.Should().NotBeNull();
        group!.CheckoutGroupId.Should().NotBe(Guid.Empty);
        group.ParentOrderId.Should().Be(500);
        group.Splits.Should().HaveCount(2);
        group.Splits.Select(split => split.VendorId).Should().BeEquivalentTo([1, 2]);
        group.Splits.Single(split => split.VendorId == 1).LineSubtotalExclTax.Should().Be(10m);
        group.Splits.Single(split => split.VendorId == 2).LineSubtotalExclTax.Should().Be(20m);
    }

    [Test]
    public async Task SplitOrder_Should_Be_Idempotent()
    {
        var store = new InMemorySplitStore();
        var service = CreateService(store, entitled: true, operatorVendorId: 99);
        store.Assign(101, 1);

        var lines = new[] { new VendorOrderLine { OrderItemId = 1, ProductId = 101, Quantity = 1, PriceExclTax = 10m } };
        var first = await service.SplitOrderAsync(501, DateTime.UtcNow, lines, CancellationToken.None);
        var second = await service.SplitOrderAsync(501, DateTime.UtcNow, lines, CancellationToken.None);

        first!.CheckoutGroupId.Should().Be(second!.CheckoutGroupId);
        store.SaveCount.Should().Be(1);
    }

    [Test]
    public async Task SplitOrder_Should_Use_Operator_Vendor_For_Unmapped_Products()
    {
        var store = new InMemorySplitStore();
        var service = CreateService(store, entitled: true, operatorVendorId: 99);

        var group = await service.SplitOrderAsync(502, DateTime.UtcNow, [
            new VendorOrderLine { OrderItemId = 1, ProductId = 777, Quantity = 1, PriceExclTax = 15m }
        ], CancellationToken.None);

        group!.Splits.Should().ContainSingle(split => split.VendorId == 99);
    }

    [Test]
    public async Task MapShipment_Should_Associate_Vendors_Present_In_Shipment()
    {
        var store = new InMemorySplitStore();
        var service = CreateService(store, entitled: true, operatorVendorId: 99);
        store.Assign(101, 1);
        store.Assign(102, 2);

        await service.MapShipmentAsync(
            900,
            600,
            [
                new ShipmentItemRef { OrderItemId = 1, Quantity = 1 },
                new ShipmentItemRef { OrderItemId = 2, Quantity = 1 }
            ],
            [
                new VendorOrderLine { OrderItemId = 1, ProductId = 101, Quantity = 1, PriceExclTax = 10m },
                new VendorOrderLine { OrderItemId = 2, ProductId = 102, Quantity = 1, PriceExclTax = 20m }
            ],
            CancellationToken.None);

        store.ShipmentMaps.Should().HaveCount(2);
        store.ShipmentMaps.Select(map => map.VendorId).Should().BeEquivalentTo([1, 2]);
    }

    [Test]
    public async Task SplitOrder_Should_NoOp_Without_Marketplace_Entitlement()
    {
        var store = new InMemorySplitStore();
        var service = CreateService(store, entitled: false, operatorVendorId: 99);
        store.Assign(101, 1);

        var group = await service.SplitOrderAsync(503, DateTime.UtcNow, [
            new VendorOrderLine { OrderItemId = 1, ProductId = 101, Quantity = 1, PriceExclTax = 10m }
        ], CancellationToken.None);

        group.Should().BeNull();
        store.SaveCount.Should().Be(0);
    }

    private static OrderVendorSplitService CreateService(InMemorySplitStore store, bool entitled, int operatorVendorId)
    {
        return new OrderVendorSplitService(
            store,
            store,
            new StubVendorRepository(operatorVendorId),
            new MarketplaceLicenceGate(new StubLicenceService(entitled)));
    }

    private sealed class InMemorySplitStore : IOrderVendorSplitStore, IVendorOwnershipStore
    {
        private readonly Dictionary<int, int> _productVendors = [];
        private OrderCheckoutGroup? _group;
        public int SaveCount { get; private set; }
        public List<ShipmentVendorMap> ShipmentMaps { get; } = [];

        public void Assign(int productId, int vendorId) => _productVendors[productId] = vendorId;

        public Task AssignProductAsync(int vendorId, int productId, CancellationToken cancellationToken)
        {
            _productVendors[productId] = vendorId;
            return Task.CompletedTask;
        }

        public Task<int?> GetProductVendorIdAsync(int productId, CancellationToken cancellationToken)
            => Task.FromResult(_productVendors.TryGetValue(productId, out var vendorId) ? (int?)vendorId : null);

        public Task<IReadOnlyList<int>> GetProductIdsAsync(int vendorId, CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyList<int>>(_productVendors.Where(pair => pair.Value == vendorId).Select(pair => pair.Key).ToList());

        public Task<IReadOnlyList<int>> GetAllMappedProductIdsAsync(CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyList<int>>(_productVendors.Keys.ToList());

        public Task<int> AssignUnmappedProductsAsync(int operatorVendorId, IReadOnlyList<int> productIds, CancellationToken cancellationToken)
            => Task.FromResult(0);

        public Task<bool> ExistsForOrderAsync(int orderId, CancellationToken cancellationToken)
            => Task.FromResult(_group?.ParentOrderId == orderId);

        public Task SaveCheckoutGroupAsync(OrderCheckoutGroup group, CancellationToken cancellationToken)
        {
            SaveCount++;
            _group = group;
            return Task.CompletedTask;
        }

        public Task<OrderCheckoutGroup?> GetByOrderIdAsync(int orderId, CancellationToken cancellationToken)
            => Task.FromResult(_group?.ParentOrderId == orderId ? _group : null);

        public Task<bool> ShipmentMapExistsAsync(int shipmentId, CancellationToken cancellationToken)
            => Task.FromResult(ShipmentMaps.Any(map => map.ShipmentId == shipmentId));

        public Task SaveShipmentVendorMapsAsync(IReadOnlyCollection<ShipmentVendorMap> maps, CancellationToken cancellationToken)
        {
            ShipmentMaps.AddRange(maps);
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<ShipmentVendorMap>> GetShipmentMapsByOrderIdAsync(int orderId, CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyList<ShipmentVendorMap>>(ShipmentMaps.Where(map => map.OrderId == orderId).ToList());
    }

    private sealed class StubVendorRepository : IVendorRepository
    {
        private readonly int _operatorVendorId;

        public StubVendorRepository(int operatorVendorId) => _operatorVendorId = operatorVendorId;

        public Task<int> InsertAsync(Vendor vendor, CancellationToken cancellationToken) => Task.FromResult(0);
        public Task UpdateAsync(Vendor vendor, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task<Vendor?> GetByIdAsync(int id, CancellationToken cancellationToken) => Task.FromResult<Vendor?>(null);
        public Task<IReadOnlyList<Vendor>> GetByStatusesAsync(IReadOnlyCollection<VendorStatus> statuses, CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyList<Vendor>>([]);
        public Task<Vendor?> GetOperatorAsync(CancellationToken cancellationToken)
            => Task.FromResult<Vendor?>(new Vendor { Id = _operatorVendorId, IsOperator = true, Status = VendorStatus.Active });
        public Task<Vendor?> GetByApplicantCustomerIdAsync(int customerId, CancellationToken cancellationToken) => Task.FromResult<Vendor?>(null);
        public Task InsertAgreementAsync(VendorAgreementAcceptance acceptance, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task<VendorAgreementAcceptance?> GetLatestAgreementAsync(int vendorId, CancellationToken cancellationToken) => Task.FromResult<VendorAgreementAcceptance?>(null);
        public Task<bool> HasAcceptedAgreementAsync(int vendorId, string agreementVersion, CancellationToken cancellationToken) => Task.FromResult(false);
        public Task<string?> GetApplicantAccessTokenHashAsync(int vendorId, CancellationToken cancellationToken) => Task.FromResult<string?>(null);
    }

    private sealed class StubLicenceService : ILicenceService
    {
        private readonly bool _entitled;

        public StubLicenceService(bool entitled) => _entitled = entitled;

        public Task<LicenceStatus> GetStatusAsync(CancellationToken cancellationToken)
            => Task.FromResult(new LicenceStatus
            {
                IsActive = _entitled,
                State = _entitled ? "active" : "inactive",
                AllowsAdminWrite = _entitled,
                MarketplaceModuleEntitlement = _entitled,
                Tier = _entitled ? LicenceTier.Business : LicenceTier.SingleStore
            });

        public Task<LicenceStatus> ActivateAsync(string licenceKey, CancellationToken cancellationToken)
            => GetStatusAsync(cancellationToken);

        public Task<LicenceStatus> HeartbeatAsync(CancellationToken cancellationToken)
            => GetStatusAsync(cancellationToken);
    }
}
