namespace TwinParticles.CheckEngine.Domain.Workshop;

public sealed class WorkshopJobVehicle
{
    public int Id { get; set; }

    public int JobId { get; set; }

    public int VehicleConfigurationId { get; set; }

    public string? Vin { get; set; }

    public string? Label { get; set; }
}
