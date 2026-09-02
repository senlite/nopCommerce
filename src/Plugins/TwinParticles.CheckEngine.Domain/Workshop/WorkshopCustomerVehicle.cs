namespace TwinParticles.CheckEngine.Domain.Workshop;

public sealed class WorkshopCustomerVehicle
{
    public int Id { get; set; }

    public int WorkshopCustomerId { get; set; }

    public int VehicleConfigurationId { get; set; }

    public string? Vin { get; set; }
}
