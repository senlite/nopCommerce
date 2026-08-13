using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TwinParticles.CheckEngine.Domain.Garage;
using TwinParticles.CheckEngine.Domain.Security;

namespace TwinParticles.CheckEngine.Application.Garage;

/// <summary>
/// Subject-access and erasure boundary for garage personal data (FR-710, FR-716, FR-960). Export
/// returns decrypted VINs to the authenticated subject; audit records contain metadata only.
/// </summary>
public sealed class GaragePrivacyService
{
    private readonly IGarageRepository _repository;
    private readonly ICheckEngineAuditService _auditService;

    public GaragePrivacyService(IGarageRepository repository, ICheckEngineAuditService auditService)
    {
        _repository = repository;
        _auditService = auditService;
    }

    public async Task<GaragePrivacyExport> ExportAsync(
        int customerId,
        string actor,
        CancellationToken cancellationToken)
    {
        var garage = await _repository.GetByCustomerIdAsync(customerId, cancellationToken);
        var export = new GaragePrivacyExport
        {
            CustomerId = customerId,
            ExportedUtc = DateTime.UtcNow,
            Vehicles = garage?.Vehicles.Select(vehicle => new GaragePrivacyVehicle
            {
                VehicleConfigurationId = vehicle.VehicleConfigurationId,
                Vin = vehicle.Vin,
                Label = vehicle.Label,
                IsActive = vehicle.IsActive,
                CreatedUtc = vehicle.CreatedUtc
            }).ToList() ?? [],
            Oems = garage?.Oems.Select(oem => new GaragePrivacyOem
            {
                OemNumberId = oem.OemNumberId,
                DisplayNumber = oem.DisplayNumber ?? string.Empty,
                CreatedUtc = oem.CreatedUtc
            }).ToList() ?? []
        };

        await _auditService.AppendAsync(
            actor,
            "garage.privacy.export",
            "CustomerGarage",
            customerId.ToString(),
            beforeJson: null,
            afterJson: $"{{\"vehicleCount\":{export.Vehicles.Count},\"oemCount\":{export.Oems.Count}}}",
            cancellationToken);
        return export;
    }

    public async Task<bool> EraseAsync(
        int customerId,
        string actor,
        CancellationToken cancellationToken)
    {
        var garage = await _repository.GetByCustomerIdAsync(customerId, cancellationToken);
        var vehicleCount = garage?.Vehicles.Count ?? 0;
        var oemCount = garage?.Oems.Count ?? 0;
        await _repository.DeleteByCustomerIdAsync(customerId, cancellationToken);

        await _auditService.AppendAsync(
            actor,
            "garage.privacy.erase",
            "CustomerGarage",
            customerId.ToString(),
            beforeJson: $"{{\"vehicleCount\":{vehicleCount},\"oemCount\":{oemCount}}}",
            afterJson: $"{{\"erased\":true,\"existed\":{(garage is not null ? "true" : "false")}}}",
            cancellationToken);
        return true;
    }
}
