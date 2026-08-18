using System.Collections.Concurrent;
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
using TwinParticles.CheckEngine.Infrastructure.Security;

namespace TwinParticles.CheckEngine.Tests.Architecture;

[TestFixture]
public class VendorIsolationServiceTests
{
    [Test]
    public async Task Vendor_A_Should_Be_Denied_Editing_Vendor_B_Product()
    {
        var harness = Harness.Create();
        await harness.Ownership.AssignProductAsync(1, 100, CancellationToken.None);
        await harness.Ownership.AssignProductAsync(2, 200, CancellationToken.None);

        var denied = await harness.Service.AuthorizeProductAsync(VendorActor.Vendor(1), 200, write: true, CancellationToken.None);
        var allowed = await harness.Service.AuthorizeProductAsync(VendorActor.Vendor(2), 200, write: true, CancellationToken.None);

        denied.Allowed.Should().BeFalse();
        denied.ReasonCode.Should().Be(VendorErrorCodes.IsolationDenied);
        allowed.Allowed.Should().BeTrue();
        harness.Audit.Snapshot().Should().Contain(entry =>
            entry.Action == "vendor.isolation.write_denied" && entry.EntityId == "200");
    }

    [Test]
    public async Task Vendor_Should_Only_See_Own_Orders_And_Buyers()
    {
        var harness = Harness.Create();
        await harness.Ownership.AssignProductAsync(1, 100, CancellationToken.None);
        await harness.Ownership.AssignProductAsync(2, 200, CancellationToken.None);
        harness.Orders.Add(new VendorOrderRecord { OrderId = 10, CustomerId = 501, ProductIds = [100] });
        harness.Orders.Add(new VendorOrderRecord { OrderId = 20, CustomerId = 502, ProductIds = [200] });

        var vendorA = VendorActor.Vendor(1);
        (await harness.Service.ListOrdersAsync(vendorA, CancellationToken.None)).Select(order => order.OrderId).Should().Equal(10);
        (await harness.Service.ListCustomersAsync(vendorA, CancellationToken.None)).Should().Equal(501);
        (await harness.Service.AuthorizeOrderAsync(vendorA, 20, CancellationToken.None)).Allowed.Should().BeFalse();
        (await harness.Service.AuthorizeCustomerAsync(vendorA, 502, CancellationToken.None)).Allowed.Should().BeFalse();
        (await harness.Service.AuthorizeCustomerAsync(vendorA, 501, CancellationToken.None)).Allowed.Should().BeTrue();
    }

    [Test]
    public async Task Operator_Admin_Should_Bypass_Isolation()
    {
        var harness = Harness.Create();
        await harness.Ownership.AssignProductAsync(1, 100, CancellationToken.None);
        await harness.Ownership.AssignProductAsync(2, 200, CancellationToken.None);

        var admin = VendorActor.OperatorAdmin;
        (await harness.Service.AuthorizeProductAsync(admin, 200, write: true, CancellationToken.None)).Allowed.Should().BeTrue();
        (await harness.Service.ListCatalogAsync(admin, CancellationToken.None)).Should().BeEquivalentTo([100, 200]);
    }

    [Test]
    public async Task Anonymous_Actor_Should_Be_Denied()
    {
        var harness = Harness.Create();
        await harness.Ownership.AssignProductAsync(1, 100, CancellationToken.None);

        var decision = await harness.Service.AuthorizeProductAsync(VendorActor.Anonymous, 100, write: false, CancellationToken.None);

        decision.ReasonCode.Should().Be(VendorErrorCodes.IsolationUnauthenticated);
    }

    private sealed class Harness
    {
        public required VendorIsolationService Service { get; init; }
        public required InMemoryOwnership Ownership { get; init; }
        public required InMemoryOrders Orders { get; init; }
        public required InMemoryCheckEngineAuditService Audit { get; init; }

        public static Harness Create()
        {
            var ownership = new InMemoryOwnership();
            var orders = new InMemoryOrders();
            var audit = new InMemoryCheckEngineAuditService();
            var service = new VendorIsolationService(
                ownership,
                orders,
                new MarketplaceLicenceGate(new StubLicenceService()),
                audit);

            return new Harness { Service = service, Ownership = ownership, Orders = orders, Audit = audit };
        }
    }

    private sealed class InMemoryOwnership : IVendorOwnershipStore
    {
        private readonly ConcurrentDictionary<int, int> _map = new();

        public Task AssignProductAsync(int vendorId, int productId, CancellationToken cancellationToken)
        {
            _map[productId] = vendorId;
            return Task.CompletedTask;
        }

        public Task<int?> GetProductVendorIdAsync(int productId, CancellationToken cancellationToken)
            => Task.FromResult(_map.TryGetValue(productId, out var vendorId) ? vendorId : (int?)null);

        public Task<IReadOnlyList<int>> GetProductIdsAsync(int vendorId, CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyList<int>>(_map.Where(pair => pair.Value == vendorId).Select(pair => pair.Key).OrderBy(id => id).ToList());

        public Task<IReadOnlyList<int>> GetAllMappedProductIdsAsync(CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyList<int>>(_map.Keys.OrderBy(id => id).ToList());

        public Task<int> AssignUnmappedProductsAsync(int operatorVendorId, IReadOnlyList<int> productIds, CancellationToken cancellationToken)
            => Task.FromResult(0);
    }

    private sealed class InMemoryOrders : IVendorOrderReadStore
    {
        private readonly List<VendorOrderRecord> _orders = [];

        public void Add(VendorOrderRecord record) => _orders.Add(record);

        public Task<IReadOnlyList<VendorOrderRecord>> GetOrdersForProductsAsync(
            IReadOnlyCollection<int> productIds,
            CancellationToken cancellationToken)
        {
            var owned = _orders
                .Where(order => order.ProductIds.Any(productIds.Contains))
                .ToList();
            return Task.FromResult<IReadOnlyList<VendorOrderRecord>>(owned);
        }
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
