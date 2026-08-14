using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TwinParticles.CheckEngine.Application.Oem;
using TwinParticles.CheckEngine.Application.Vehicle.Vin;
using TwinParticles.CheckEngine.Domain.Garage;

namespace TwinParticles.CheckEngine.Application.Garage;

public sealed class GarageService
{
    private readonly IGarageAuditService _auditService;
    private readonly IGarageGuestStore _guestStore;
    private readonly IGarageRepository _repository;
    private readonly OemResolveService _oemResolveService;
    private readonly VinDecodeApplicationService _vinDecodeService;

    public GarageService(
        IGarageRepository repository,
        IGarageGuestStore guestStore,
        IGarageAuditService auditService,
        VinDecodeApplicationService vinDecodeService,
        OemResolveService oemResolveService)
    {
        _repository = repository;
        _guestStore = guestStore;
        _auditService = auditService;
        _vinDecodeService = vinDecodeService;
        _oemResolveService = oemResolveService;
    }

    public async Task<Domain.Garage.Garage> GetAsync(int customerId, CancellationToken cancellationToken)
    {
        return await _repository.GetOrCreateAsync(customerId, cancellationToken);
    }

    public async Task<GarageVehicle> AddVehicleAsync(int customerId, int? vehicleConfigurationId, string? vin, string? label, CancellationToken cancellationToken)
    {
        var garage = await _repository.GetOrCreateAsync(customerId, cancellationToken);

        var normalizedVin = string.IsNullOrWhiteSpace(vin) ? null : vin.Trim().ToUpperInvariant();
        int? resolvedConfigurationId = vehicleConfigurationId;

        if (!string.IsNullOrWhiteSpace(normalizedVin))
        {
            var decode = await _vinDecodeService.DecodeAsync(normalizedVin, cancellationToken);
            if (!resolvedConfigurationId.HasValue &&
                string.Equals(decode.Outcome, "NeedsDisambiguation", StringComparison.Ordinal))
            {
                throw new GarageVinDisambiguationException(decode.Candidates);
            }

            if (!resolvedConfigurationId.HasValue &&
                string.Equals(decode.Outcome, "SingleMatch", StringComparison.Ordinal) &&
                decode.Candidates.Count == 1)
            {
                resolvedConfigurationId = decode.Candidates[0].VehicleConfigurationId;
            }
        }

        var id = garage.Vehicles.Count == 0 ? 1 : garage.Vehicles.Max(x => x.Id) + 1;
        var vehicle = new GarageVehicle
        {
            Id = id,
            GarageId = garage.Id,
            VehicleConfigurationId = resolvedConfigurationId,
            Vin = normalizedVin,
            Label = string.IsNullOrWhiteSpace(label) ? BuildLabel(resolvedConfigurationId, normalizedVin) : label!.Trim(),
            IsActive = garage.ActiveGarageVehicleId is null,
            CreatedUtc = DateTime.UtcNow
        };

        if (vehicle.IsActive)
            garage.ActiveGarageVehicleId = vehicle.Id;

        garage.Vehicles.Add(vehicle);
        garage.UpdatedUtc = DateTime.UtcNow;

        await _repository.SaveAsync(garage, cancellationToken);
        return vehicle;
    }

    public async Task<bool> SetActiveVehicleAsync(int customerId, int garageVehicleId, CancellationToken cancellationToken)
    {
        var garage = await _repository.GetByCustomerIdAsync(customerId, cancellationToken);
        if (garage is null)
            return false;

        var target = garage.Vehicles.FirstOrDefault(x => x.Id == garageVehicleId);
        if (target is null)
            return false;

        foreach (var vehicle in garage.Vehicles)
            vehicle.IsActive = false;

        target.IsActive = true;
        garage.ActiveGarageVehicleId = target.Id;
        garage.UpdatedUtc = DateTime.UtcNow;

        await _repository.SaveAsync(garage, cancellationToken);
        return true;
    }

    public async Task<bool> ClearActiveVehicleAsync(int customerId, bool confirmed, CancellationToken cancellationToken)
    {
        if (!confirmed)
            return false;

        var garage = await _repository.GetByCustomerIdAsync(customerId, cancellationToken);
        if (garage is null)
            return false;

        foreach (var vehicle in garage.Vehicles)
            vehicle.IsActive = false;

        garage.ActiveGarageVehicleId = null;
        garage.UpdatedUtc = DateTime.UtcNow;
        await _repository.SaveAsync(garage, cancellationToken);
        return true;
    }

    public async Task<bool> RemoveVehicleAsync(int customerId, int garageVehicleId, CancellationToken cancellationToken)
    {
        var garage = await _repository.GetByCustomerIdAsync(customerId, cancellationToken);
        if (garage is null)
            return false;

        var target = garage.Vehicles.FirstOrDefault(x => x.Id == garageVehicleId);
        if (target is null)
            return false;

        var wasActive = target.IsActive || garage.ActiveGarageVehicleId == garageVehicleId;
        garage.Vehicles.Remove(target);

        if (wasActive)
        {
            foreach (var vehicle in garage.Vehicles)
                vehicle.IsActive = false;

            var next = garage.Vehicles.FirstOrDefault();
            if (next is not null)
            {
                next.IsActive = true;
                garage.ActiveGarageVehicleId = next.Id;
            }
            else
            {
                garage.ActiveGarageVehicleId = null;
            }
        }

        garage.UpdatedUtc = DateTime.UtcNow;
        await _repository.SaveAsync(garage, cancellationToken);
        return true;
    }

    public async Task<bool> SaveOemAsync(int customerId, string oemNumber, int? manufacturerId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(oemNumber))
            return false;

        var garage = await _repository.GetOrCreateAsync(customerId, cancellationToken);
        var resolved = await _oemResolveService.ResolveAsync(new OemResolveQuery
        {
            Number = oemNumber,
            ManufacturerId = manufacturerId
        }, cancellationToken);

        if (!resolved.Success || !resolved.OemNumberId.HasValue)
            return false;

        if (garage.Oems.Any(x => x.OemNumberId == resolved.OemNumberId.Value))
            return true;

        garage.Oems.Add(new GarageOem
        {
            Id = garage.Oems.Count == 0 ? 1 : garage.Oems.Max(x => x.Id) + 1,
            GarageId = garage.Id,
            OemNumberId = resolved.OemNumberId.Value,
            DisplayNumber = resolved.DisplayNumber ?? oemNumber.Trim(),
            CreatedUtc = DateTime.UtcNow
        });

        garage.UpdatedUtc = DateTime.UtcNow;
        await _repository.SaveAsync(garage, cancellationToken);
        return true;
    }

    public async Task<bool> MigrateGuestAsync(int customerId, string guestKey, CancellationToken cancellationToken)
    {
        return await MigrateGuestAsync(customerId, guestKey, inlinePayload: null, cancellationToken);
    }

    /// <summary>
    /// Merges the browser-local guest garage into the authenticated customer's SQL garage.
    /// The inline payload is authoritative when supplied; the process-local guest store remains
    /// only as a backwards-compatible fallback for older callers and tests.
    /// </summary>
    public async Task<bool> MigrateGuestAsync(
        int customerId,
        string guestKey,
        GarageGuestPayload? inlinePayload,
        CancellationToken cancellationToken)
    {
        var payload = inlinePayload ?? await _guestStore.GetAsync(guestKey, cancellationToken);
        if (payload is null || (payload.Vehicles.Count == 0 && payload.Oems.Count == 0))
            return false;

        var garage = await _repository.GetOrCreateAsync(customerId, cancellationToken);

        var migratedVehicleIdByGuestVehicleId = new Dictionary<int, int>();
        for (var index = 0; index < payload.Vehicles.Count; index++)
        {
            var guestVehicle = payload.Vehicles[index];
            var guestVehicleId = guestVehicle.Id > 0 ? guestVehicle.Id : index + 1;
            var normalizedVin = NormalizeVin(guestVehicle.Vin);

            var existingVehicle = garage.Vehicles.FirstOrDefault(x =>
                (guestVehicle.VehicleConfigurationId.HasValue &&
                 x.VehicleConfigurationId == guestVehicle.VehicleConfigurationId) ||
                (!string.IsNullOrWhiteSpace(normalizedVin) &&
                 string.Equals(NormalizeVin(x.Vin), normalizedVin, StringComparison.Ordinal)));

            if (existingVehicle is not null)
            {
                migratedVehicleIdByGuestVehicleId[guestVehicleId] = existingVehicle.Id;
                continue;
            }

            var nextId = garage.Vehicles.Count == 0 ? 1 : garage.Vehicles.Max(x => x.Id) + 1;
            garage.Vehicles.Add(new GarageVehicle
            {
                Id = nextId,
                GarageId = garage.Id,
                VehicleConfigurationId = guestVehicle.VehicleConfigurationId,
                Vin = normalizedVin,
                Label = string.IsNullOrWhiteSpace(guestVehicle.Label)
                    ? BuildLabel(guestVehicle.VehicleConfigurationId, normalizedVin)
                    : guestVehicle.Label.Trim(),
                IsActive = false,
                CreatedUtc = DateTime.UtcNow
            });

            migratedVehicleIdByGuestVehicleId[guestVehicleId] = nextId;
        }

        foreach (var guestOem in payload.Oems)
        {
            if (garage.Oems.Any(x => x.OemNumberId == guestOem.OemNumberId))
                continue;

            var nextId = garage.Oems.Count == 0 ? 1 : garage.Oems.Max(x => x.Id) + 1;
            garage.Oems.Add(new GarageOem
            {
                Id = nextId,
                GarageId = garage.Id,
                OemNumberId = guestOem.OemNumberId,
                DisplayNumber = guestOem.DisplayNumber,
                CreatedUtc = DateTime.UtcNow
            });
        }

        if (!garage.ActiveGarageVehicleId.HasValue && payload.ActiveVehicleId.HasValue)
        {
            var mappedActiveId = migratedVehicleIdByGuestVehicleId.GetValueOrDefault(payload.ActiveVehicleId.Value);
            if (mappedActiveId > 0)
            {
                var active = garage.Vehicles.FirstOrDefault(x => x.Id == mappedActiveId);
                if (active is not null)
                {
                    active.IsActive = true;
                    garage.ActiveGarageVehicleId = active.Id;
                }
            }
        }

        garage.UpdatedUtc = DateTime.UtcNow;
        await _repository.SaveAsync(garage, cancellationToken);
        await _guestStore.RemoveAsync(guestKey, cancellationToken);

        return true;
    }

    public async Task<Domain.Garage.Garage?> AdminViewAsync(int customerId, CancellationToken cancellationToken)
    {
        var garage = await _repository.GetByCustomerIdAsync(customerId, cancellationToken);
        if (garage is not null)
            _auditService.RecordAdminView(customerId);

        return garage;
    }

    public async Task<GarageGuestPayload> GetGuestAsync(string guestKey, CancellationToken cancellationToken)
    {
        return await _guestStore.GetAsync(guestKey, cancellationToken) ?? new GarageGuestPayload();
    }

    public Task SetGuestAsync(string guestKey, GarageGuestPayload payload, CancellationToken cancellationToken)
    {
        return _guestStore.SetAsync(guestKey, payload, cancellationToken);
    }

    private static string BuildLabel(int? vehicleConfigurationId, string? vin)
    {
        if (vehicleConfigurationId.HasValue)
            return $"Vehicle #{vehicleConfigurationId.Value}";

        if (!string.IsNullOrWhiteSpace(vin))
            return $"VIN {vin}";

        return "Garage Vehicle";
    }

    private static string? NormalizeVin(string? vin)
        => string.IsNullOrWhiteSpace(vin) ? null : vin.Trim().ToUpperInvariant();
}
