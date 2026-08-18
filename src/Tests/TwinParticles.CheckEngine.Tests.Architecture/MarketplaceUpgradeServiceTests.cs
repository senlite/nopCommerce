using System;
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
using TwinParticles.CheckEngine.Domain.Performance;
using TwinParticles.CheckEngine.Infrastructure.Security;

namespace TwinParticles.CheckEngine.Tests.Architecture;

[TestFixture]
public class MarketplaceUpgradeServiceTests
{
    [Test]
    public async Task Enable_Should_Assign_Existing_Catalog_To_Operator_Without_Loss()
    {
        var harness = Harness.Create(entitled: true, productIds: [11, 22, 33]);

        var result = await harness.Service.EnableAsync(CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        result.CatalogPreserved.Should().BeTrue();
        result.ProductCount.Should().Be(3);
        result.NewlyAssigned.Should().Be(3);
        result.AlreadyAssigned.Should().Be(0);
        foreach (var productId in new[] { 11, 22, 33 })
            (await harness.Ownership.GetProductVendorIdAsync(productId, CancellationToken.None)).Should().Be(result.OperatorVendorId);
    }

    [Test]
    public async Task Enable_Should_Be_Idempotent_And_Keep_Existing_Vendor_Assignments()
    {
        var harness = Harness.Create(entitled: true, productIds: [11, 22, 33]);
        var first = await harness.Service.EnableAsync(CancellationToken.None);
        await harness.Ownership.AssignProductAsync(99, 22, CancellationToken.None);

        var second = await harness.Service.EnableAsync(CancellationToken.None);

        second.Succeeded.Should().BeTrue();
        second.OperatorVendorId.Should().Be(first.OperatorVendorId);
        second.NewlyAssigned.Should().Be(0);
        (await harness.Ownership.GetProductVendorIdAsync(11, CancellationToken.None)).Should().Be(first.OperatorVendorId);
        (await harness.Ownership.GetProductVendorIdAsync(22, CancellationToken.None)).Should().Be(99);
        (await harness.Ownership.GetProductVendorIdAsync(33, CancellationToken.None)).Should().Be(first.OperatorVendorId);
    }

    [Test]
    public async Task Enable_Should_Deny_Without_Marketplace_Entitlement()
    {
        var harness = Harness.Create(entitled: false, productIds: [11]);

        var result = await harness.Service.EnableAsync(CancellationToken.None);

        result.Succeeded.Should().BeFalse();
        result.ReasonCode.Should().Be(VendorErrorCodes.LicenceDenied);
        harness.Vendors.Count.Should().Be(0);
    }

    private sealed class Harness
    {
        public required MarketplaceUpgradeService Service { get; init; }
        public required InMemoryOwnership Ownership { get; init; }
        public required InMemoryVendors Vendors { get; init; }

        public static Harness Create(bool entitled, IReadOnlyList<int> productIds)
        {
            var vendors = new InMemoryVendors();
            var ownership = new InMemoryOwnership();
            var service = new MarketplaceUpgradeService(
                vendors,
                ownership,
                new StaticCatalog(productIds),
                new MarketplaceLicenceGate(new StubLicenceService(entitled)),
                new FixedClock(new DateTimeOffset(2026, 8, 17, 12, 0, 0, TimeSpan.Zero)),
                new InMemoryCheckEngineAuditService(),
                new NoOpVehicleSeedLoader());

            return new Harness { Service = service, Ownership = ownership, Vendors = vendors };
        }
    }

    private sealed class StaticCatalog : IVendorCommerceCatalog
    {
        private readonly IReadOnlyList<int> _ids;

        public StaticCatalog(IReadOnlyList<int> ids) => _ids = ids;

        public Task<IReadOnlyList<int>> ListSellableProductIdsAsync(CancellationToken cancellationToken)
            => Task.FromResult(_ids);
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

        public async Task<int> AssignUnmappedProductsAsync(int operatorVendorId, IReadOnlyList<int> productIds, CancellationToken cancellationToken)
        {
            var assigned = 0;
            foreach (var productId in productIds)
            {
                if (_map.ContainsKey(productId))
                    continue;
                await AssignProductAsync(operatorVendorId, productId, cancellationToken);
                assigned++;
            }

            return assigned;
        }
    }

    private sealed class InMemoryVendors : IVendorRepository
    {
        private readonly ConcurrentDictionary<int, Vendor> _vendors = new();
        private int _nextId = 1;

        public int Count => _vendors.Count;

        public Task<int> InsertAsync(Vendor vendor, CancellationToken cancellationToken)
        {
            vendor.Id = Interlocked.Increment(ref _nextId);
            _vendors[vendor.Id] = vendor;
            return Task.FromResult(vendor.Id);
        }

        public Task UpdateAsync(Vendor vendor, CancellationToken cancellationToken)
        {
            _vendors[vendor.Id] = vendor;
            return Task.CompletedTask;
        }

        public Task<Vendor?> GetByIdAsync(int id, CancellationToken cancellationToken)
            => Task.FromResult(_vendors.TryGetValue(id, out var vendor) ? vendor : null);

        public Task<IReadOnlyList<Vendor>> GetByStatusesAsync(IReadOnlyCollection<VendorStatus> statuses, CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyList<Vendor>>(_vendors.Values.Where(vendor => statuses.Contains(vendor.Status)).ToList());

        public Task<Vendor?> GetOperatorAsync(CancellationToken cancellationToken)
            => Task.FromResult(_vendors.Values.FirstOrDefault(vendor => vendor.IsOperator));

        public Task<Vendor?> GetByApplicantCustomerIdAsync(int customerId, CancellationToken cancellationToken)
            => Task.FromResult(_vendors.Values.FirstOrDefault(vendor => vendor.ApplicantCustomerId == customerId));

        public Task InsertAgreementAsync(VendorAgreementAcceptance acceptance, CancellationToken cancellationToken)
            => Task.CompletedTask;

        public Task<VendorAgreementAcceptance?> GetLatestAgreementAsync(int vendorId, CancellationToken cancellationToken)
            => Task.FromResult<VendorAgreementAcceptance?>(null);

        public Task<bool> HasAcceptedAgreementAsync(int vendorId, string agreementVersion, CancellationToken cancellationToken)
            => Task.FromResult(false);

        public Task<string?> GetApplicantAccessTokenHashAsync(int vendorId, CancellationToken cancellationToken)
            => Task.FromResult<string?>(null);
    }

    private sealed class NoOpVehicleSeedLoader : TwinParticles.CheckEngine.Domain.Vehicle.Admin.IVehicleSeedLoader
    {
        public Task<TwinParticles.CheckEngine.Domain.Vehicle.Admin.VehicleSeedLoadResult> SeedAsync(CancellationToken cancellationToken)
            => Task.FromResult(new TwinParticles.CheckEngine.Domain.Vehicle.Admin.VehicleSeedLoadResult());
    }

    private sealed class StubLicenceService : ILicenceService
    {
        private readonly bool _entitled;

        public StubLicenceService(bool entitled) => _entitled = entitled;

        public Task<LicenceStatus> GetStatusAsync(CancellationToken cancellationToken)
            => Task.FromResult(new LicenceStatus
            {
                IsActive = true,
                State = "active",
                AllowsAdminWrite = true,
                MarketplaceModuleEntitlement = _entitled,
                Tier = _entitled ? LicenceTier.Business : LicenceTier.SingleStore
            });

        public Task<LicenceStatus> ActivateAsync(string licenceKey, CancellationToken cancellationToken)
            => GetStatusAsync(cancellationToken);

        public Task<LicenceStatus> HeartbeatAsync(CancellationToken cancellationToken)
            => GetStatusAsync(cancellationToken);
    }

    private sealed class FixedClock : ICheckEngineClock
    {
        public FixedClock(DateTimeOffset utcNow) => UtcNow = utcNow;

        public DateTimeOffset UtcNow { get; }
    }
}
