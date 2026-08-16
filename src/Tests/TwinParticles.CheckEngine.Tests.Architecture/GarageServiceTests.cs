using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using NUnit.Framework;
using TwinParticles.CheckEngine.Application.Garage;
using TwinParticles.CheckEngine.Application.Oem;
using TwinParticles.CheckEngine.Application.Vehicle.Vin;
using TwinParticles.CheckEngine.Domain.Garage;
using TwinParticles.CheckEngine.Domain.Observability;
using TwinParticles.CheckEngine.Domain.Oem;
using TwinParticles.CheckEngine.Domain.Vehicle;

namespace TwinParticles.CheckEngine.Tests.Architecture;

[TestFixture]
public class GarageServiceTests
{
    [Test]
    public async Task SetActiveVehicleAsync_Should_Keep_Exactly_One_Active()
    {
        var service = CreateService();
        await service.AddVehicleAsync(15, 1001, null, "Car A", CancellationToken.None);
        await service.AddVehicleAsync(15, 1002, null, "Car B", CancellationToken.None);

        var set = await service.SetActiveVehicleAsync(15, 2, CancellationToken.None);
        set.Should().BeTrue();

        var garage = await service.GetAsync(15, CancellationToken.None);
        garage.ActiveGarageVehicleId.Should().Be(2);
        garage.Vehicles.Count(x => x.IsActive).Should().Be(1);
        garage.Vehicles.Single(x => x.IsActive).Id.Should().Be(2);
    }

    [Test]
    public async Task ClearActiveVehicleAsync_Should_Require_Confirmation()
    {
        var service = CreateService();
        await service.AddVehicleAsync(16, 2001, null, "Car", CancellationToken.None);

        var notConfirmed = await service.ClearActiveVehicleAsync(16, confirmed: false, CancellationToken.None);
        notConfirmed.Should().BeFalse();

        var garageAfterFailedClear = await service.GetAsync(16, CancellationToken.None);
        garageAfterFailedClear.ActiveGarageVehicleId.Should().NotBeNull();

        var confirmed = await service.ClearActiveVehicleAsync(16, confirmed: true, CancellationToken.None);
        confirmed.Should().BeTrue();

        var garage = await service.GetAsync(16, CancellationToken.None);
        garage.ActiveGarageVehicleId.Should().BeNull();
        garage.Vehicles.Any(x => x.IsActive).Should().BeFalse();
    }

    [Test]
    public async Task MigrateGuestAsync_Should_Merge_Guest_Data_And_Remove_Guest_Payload()
    {
        var service = CreateService();

        await service.SetGuestAsync("guest-1", new GarageGuestPayload
        {
            ActiveVehicleId = 1,
            Vehicles =
            [
                new GarageVehicle { Id = 1, VehicleConfigurationId = 3001, Vin = "1HGCM82633A004352", Label = "Guest Car" }
            ],
            Oems =
            [
                new GarageOem { Id = 1, OemNumberId = 901, DisplayNumber = "11-51-7-586-925" }
            ]
        }, CancellationToken.None);

        var migrated = await service.MigrateGuestAsync(42, "guest-1", CancellationToken.None);
        migrated.Should().BeTrue();

        var garage = await service.GetAsync(42, CancellationToken.None);
        garage.Vehicles.Should().ContainSingle();
        garage.Vehicles[0].VehicleConfigurationId.Should().BeNull(
            "a client-supplied configuration id is ignored unless VIN decode confirms it");
        garage.Oems.Should().ContainSingle();

        var payload = await service.GetGuestAsync("guest-1", CancellationToken.None);
        payload.Vehicles.Should().BeEmpty();
        payload.Oems.Should().BeEmpty();
    }

    [Test]
    public async Task MigrateGuestAsync_Should_Map_Guest_Active_Vehicle_To_Merged_Vehicle()
    {
        var service = CreateService();

        await service.SetGuestAsync("guest-active-map", new GarageGuestPayload
        {
            ActiveVehicleId = 9,
            Vehicles =
            [
                new GarageVehicle { Id = 9, VehicleConfigurationId = 4001, Vin = "WP0ZZZ99ZTS392124", Label = "Mapped Active" }
            ]
        }, CancellationToken.None);

        var migrated = await service.MigrateGuestAsync(50, "guest-active-map", CancellationToken.None);
        migrated.Should().BeTrue();

        var garage = await service.GetAsync(50, CancellationToken.None);
        garage.ActiveGarageVehicleId.Should().NotBeNull();
        garage.Vehicles.Count(x => x.IsActive).Should().Be(1);
        garage.Vehicles.Single(x => x.IsActive).Vin.Should().Be("WP0ZZZ99ZTS392124");
        garage.Vehicles.Single(x => x.IsActive).VehicleConfigurationId.Should().BeNull();
    }

    [Test]
    public async Task MigrateGuestAsync_Should_Not_Override_Existing_Active_Vehicle()
    {
        var service = CreateService();

        await service.AddVehicleAsync(51, 5001, null, "Existing Active", CancellationToken.None);

        await service.SetGuestAsync("guest-existing-active", new GarageGuestPayload
        {
            ActiveVehicleId = 9,
            Vehicles =
            [
                new GarageVehicle { Id = 9, VehicleConfigurationId = 5002, Vin = "WBAFR7C50CC811111", Label = "Guest Vehicle" }
            ]
        }, CancellationToken.None);

        var migrated = await service.MigrateGuestAsync(51, "guest-existing-active", CancellationToken.None);
        migrated.Should().BeTrue();

        var garage = await service.GetAsync(51, CancellationToken.None);
        garage.ActiveGarageVehicleId.Should().Be(1);
        garage.Vehicles.Count.Should().Be(2);
        garage.Vehicles.Count(x => x.IsActive).Should().Be(1);
        garage.Vehicles.Single(x => x.IsActive).Id.Should().Be(1);
    }

    [Test]
    public async Task MigrateGuestAsync_Should_Merge_Inline_Payload_When_Server_Store_Is_Empty()
    {
        var service = CreateService();
        var payload = new GarageGuestPayload
        {
            ActiveVehicleId = 7,
            Vehicles =
            [
                new GarageVehicle
                {
                    Id = 7,
                    VehicleConfigurationId = 6001,
                    Vin = " wba8e9g50gnu12345 ",
                    Label = " Guest BMW "
                }
            ]
        };

        var migrated = await service.MigrateGuestAsync(60, "browser-only-key", payload, CancellationToken.None);

        migrated.Should().BeTrue("the browser payload is authoritative and needs no process-local state");
        var garage = await service.GetAsync(60, CancellationToken.None);
        garage.Vehicles.Should().ContainSingle();
        garage.Vehicles[0].Vin.Should().Be("WBA8E9G50GNU12345");
        garage.Vehicles[0].Label.Should().Be("Guest BMW");
        garage.Vehicles[0].VehicleConfigurationId.Should().BeNull();
        garage.ActiveGarageVehicleId.Should().Be(garage.Vehicles[0].Id);
        garage.Vehicles[0].IsActive.Should().BeTrue();
    }

    [Test]
    public async Task MigrateGuestAsync_Should_Deduplicate_By_Configuration_Or_Vin()
    {
        var service = CreateService();
        await service.AddVehicleAsync(61, 7001, null, "Existing Config", CancellationToken.None);
        await service.AddVehicleAsync(61, null, "WBA00000000000001", "Existing VIN", CancellationToken.None);

        var payload = new GarageGuestPayload
        {
            Vehicles =
            [
                new GarageVehicle { Id = 1, VehicleConfigurationId = 7001, Vin = "OTHER", Label = "Same config" },
                new GarageVehicle { Id = 2, VehicleConfigurationId = 9999, Vin = " wba00000000000001 ", Label = "Same VIN" }
            ]
        };

        var migrated = await service.MigrateGuestAsync(61, "dedupe-key", payload, CancellationToken.None);

        migrated.Should().BeTrue();
        var garage = await service.GetAsync(61, CancellationToken.None);
        garage.Vehicles.Should().HaveCount(3,
            "a guest VIN that does not decode cannot piggy-back onto another vehicle's configuration id");
    }

    [Test]
    public async Task MigrateGuestAsync_Should_Reject_An_Empty_Inline_Payload()
    {
        var service = CreateService();

        var migrated = await service.MigrateGuestAsync(
            62,
            "empty-key",
            new GarageGuestPayload(),
            CancellationToken.None);

        migrated.Should().BeFalse();
    }

    [Test]
    public async Task RemoveVehicleAsync_Should_Remove_And_Promote_Another_When_Active_Removed()
    {
        var service = CreateService();
        await service.AddVehicleAsync(18, 1801, null, "Car A", CancellationToken.None);
        await service.AddVehicleAsync(18, 1802, null, "Car B", CancellationToken.None);

        var garageBefore = await service.GetAsync(18, CancellationToken.None);
        garageBefore.ActiveGarageVehicleId.Should().Be(1);

        var removed = await service.RemoveVehicleAsync(18, 1, CancellationToken.None);
        removed.Should().BeTrue();

        var garage = await service.GetAsync(18, CancellationToken.None);
        garage.Vehicles.Should().ContainSingle();
        garage.Vehicles[0].Id.Should().Be(2);
        garage.ActiveGarageVehicleId.Should().Be(2);
        garage.Vehicles.Single(x => x.IsActive).Id.Should().Be(2);
    }

    [Test]
    public async Task RemoveVehicleAsync_Should_Clear_Active_When_Last_Vehicle_Removed()
    {
        var service = CreateService();
        await service.AddVehicleAsync(19, 1901, null, "Only Car", CancellationToken.None);

        var removed = await service.RemoveVehicleAsync(19, 1, CancellationToken.None);
        removed.Should().BeTrue();

        var garage = await service.GetAsync(19, CancellationToken.None);
        garage.Vehicles.Should().BeEmpty();
        garage.ActiveGarageVehicleId.Should().BeNull();
    }

    [Test]
    public async Task AdminViewAsync_Should_Record_Audit_Entry()
    {
        var audit = new FakeGarageAuditService();
        var service = CreateService(auditService: audit);

        await service.AddVehicleAsync(7, 7001, null, "AdminViewCar", CancellationToken.None);

        var garage = await service.AdminViewAsync(7, CancellationToken.None);
        garage.Should().NotBeNull();

        audit.GetViewedCustomerIds().Should().Contain(7);
    }

    [Test]
    public async Task AddVehicleAsync_Should_Never_Put_The_Full_Vin_In_The_Label()
    {
        var service = CreateService(privacyService: new Last4PrivacyService());
        var vin = "WBA8E9G58GNT12345";

        var vehicle = await service.AddVehicleAsync(80, null, vin, null, CancellationToken.None);

        vehicle.Label.Should().Be("VIN …2345");
        vehicle.Label.Should().NotContain(vin);
        vehicle.Vin.Should().Be(vin);
        vehicle.VehicleConfigurationId.Should().BeNull();
    }

    private static GarageService CreateService(
        IGarageAuditService? auditService = null,
        IVinPrivacyService? privacyService = null)
    {
        var repository = new FakeGarageRepository();
        var guestStore = new FakeGarageGuestStore();
        var telemetry = new NoopTelemetry();

        var vinService = new VinDecodeApplicationService(new FakeVinRegistry(), telemetry);
        var oemService = new OemResolveService(
            new FakeOemNormalizationService(),
            new FakeOemSearchRepository(),
            new OemSupersessionService(new FakeOemRelationRepository()),
            new EmptyProductOemMapRepository());

        return new GarageService(
            repository,
            guestStore,
            auditService ?? new FakeGarageAuditService(),
            vinService,
            oemService,
            privacyService);
    }

    private sealed class FakeGarageRepository : IGarageRepository
    {
        private readonly Dictionary<int, Garage> _state = [];

        public Task<Garage> GetOrCreateAsync(int customerId, CancellationToken cancellationToken)
        {
            if (!_state.TryGetValue(customerId, out var garage))
            {
                garage = new Garage { Id = customerId, CustomerId = customerId };
                _state[customerId] = garage;
            }

            return Task.FromResult(garage);
        }

        public Task<Garage?> GetByCustomerIdAsync(int customerId, CancellationToken cancellationToken)
        {
            _state.TryGetValue(customerId, out var garage);
            return Task.FromResult(garage);
        }

        public Task SaveAsync(Garage garage, CancellationToken cancellationToken)
        {
            _state[garage.CustomerId] = garage;
            return Task.CompletedTask;
        }

        public Task<bool> DeleteByCustomerIdAsync(int customerId, CancellationToken cancellationToken)
            => Task.FromResult(_state.Remove(customerId));
    }

    private sealed class FakeGarageGuestStore : IGarageGuestStore
    {
        private readonly Dictionary<string, GarageGuestPayload> _state = [];

        public Task<GarageGuestPayload?> GetAsync(string guestKey, CancellationToken cancellationToken)
        {
            _state.TryGetValue(guestKey, out var payload);
            return Task.FromResult(payload);
        }

        public Task SetAsync(string guestKey, GarageGuestPayload payload, CancellationToken cancellationToken)
        {
            _state[guestKey] = payload;
            return Task.CompletedTask;
        }

        public Task RemoveAsync(string guestKey, CancellationToken cancellationToken)
        {
            _state.Remove(guestKey);
            return Task.CompletedTask;
        }
    }

    private sealed class FakeGarageAuditService : IGarageAuditService
    {
        private readonly List<int> _ids = [];

        public void RecordAdminView(int customerId)
        {
            _ids.Add(customerId);
        }

        public IReadOnlyList<int> GetViewedCustomerIds() => _ids;
    }

    private sealed class Last4PrivacyService : IVinPrivacyService
    {
        public string? GetLast4(string? normalizedVin)
            => string.IsNullOrWhiteSpace(normalizedVin) || normalizedVin.Length < 4
                ? null
                : normalizedVin[^4..];

        public string CreateHash(string normalizedVin) => normalizedVin;
    }

    private sealed class FakeVinRegistry : IVinDecoderRegistry
    {
        public IManufacturerVinDecoder? Resolve(string wmi) => null;
    }

    private sealed class NoopTelemetry : ICheckEngineTelemetry
    {
        public void TrackEvent(string eventName, IReadOnlyDictionary<string, object?> properties)
        {
        }
    }

    private sealed class FakeOemNormalizationService : IOemNormalizationService
    {
        public string Normalize(string rawNumber)
            => string.IsNullOrWhiteSpace(rawNumber) ? string.Empty : rawNumber.Replace("-", string.Empty);
    }

    private sealed class FakeOemSearchRepository : IOemSearchReadRepository
    {
        public Task<IReadOnlyList<OemNumber>> FindByNormalizedNumberAsync(string normalizedNumber, int? manufacturerId, CancellationToken cancellationToken)
        {
            if (normalizedNumber == "11517586925")
            {
                return Task.FromResult<IReadOnlyList<OemNumber>>([
                    new OemNumber { Id = 901, ManufacturerId = manufacturerId ?? 1, DisplayNumber = "11-51-7-586-925", NormalizedNumber = normalizedNumber }
                ]);
            }

            return Task.FromResult<IReadOnlyList<OemNumber>>([]);
        }
    }

    private sealed class FakeOemRelationRepository : IOemRelationReadRepository
    {
        public Task<IReadOnlyList<OemRelation>> GetActiveOutgoingRelationsAsync(int fromOemNumberId, CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyList<OemRelation>>([]);
    }

    private sealed class EmptyProductOemMapRepository : IProductOemMapRepository
    {
        public Task UpsertAsync(ProductOemMap map, CancellationToken cancellationToken)
            => Task.CompletedTask;

        public Task<IReadOnlyList<ProductOemMap>> GetByProductIdAsync(int productId, CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyList<ProductOemMap>>([]);

        public Task<IReadOnlyList<ProductOemMap>> GetByOemNumberIdAsync(int oemNumberId, CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyList<ProductOemMap>>([]);
    }
}
