using System;
using System.Collections.Generic;

namespace TwinParticles.CheckEngine.Application.Garage;

public sealed class GaragePrivacyExport
{
    public int CustomerId { get; init; }

    public DateTime ExportedUtc { get; init; }

    public IReadOnlyList<GaragePrivacyVehicle> Vehicles { get; init; } = [];

    public IReadOnlyList<GaragePrivacyOem> Oems { get; init; } = [];
}

public sealed class GaragePrivacyVehicle
{
    public int? VehicleConfigurationId { get; init; }
    public string? Vin { get; init; }
    public string Label { get; init; } = string.Empty;
    public bool IsActive { get; init; }
    public DateTime CreatedUtc { get; init; }
}

public sealed class GaragePrivacyOem
{
    public int? OemNumberId { get; init; }
    public string DisplayNumber { get; init; } = string.Empty;
    public DateTime CreatedUtc { get; init; }
}
