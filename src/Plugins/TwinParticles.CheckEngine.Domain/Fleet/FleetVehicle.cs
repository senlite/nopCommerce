namespace TwinParticles.CheckEngine.Domain.Fleet;

public sealed class FleetVehicle
{
    public int Id { get; set; }

    public int FleetAccountId { get; set; }

    public int? VehicleConfigurationId { get; set; }

    public string? Vin { get; set; }

    public string? AssetTag { get; set; }
}
