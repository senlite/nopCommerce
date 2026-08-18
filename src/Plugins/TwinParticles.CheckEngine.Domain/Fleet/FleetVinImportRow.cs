namespace TwinParticles.CheckEngine.Domain.Fleet;

public sealed class FleetVinImportRow
{
    public string Vin { get; set; } = string.Empty;

    public string Outcome { get; set; } = string.Empty;

    public int? VehicleConfigurationId { get; set; }

    public string? ReasonCode { get; set; }
}
