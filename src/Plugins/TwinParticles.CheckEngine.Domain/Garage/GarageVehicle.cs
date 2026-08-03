using System;

namespace TwinParticles.CheckEngine.Domain.Garage;

public sealed class GarageVehicle
{
    public int Id { get; set; }

    public int GarageId { get; set; }

    public int? VehicleConfigurationId { get; set; }

    public string? Vin { get; set; }

    public string Label { get; set; } = string.Empty;

    public bool IsActive { get; set; }

    public DateTime CreatedUtc { get; set; }
}
