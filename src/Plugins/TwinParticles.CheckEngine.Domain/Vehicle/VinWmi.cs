namespace TwinParticles.CheckEngine.Domain.Vehicle;

public sealed class VinWmi
{
    public int Id { get; set; }

    public string Wmi { get; set; } = string.Empty;

    public int? MakeId { get; set; }

    public string ManufacturerName { get; set; } = string.Empty;

    public string? RegionCode { get; set; }

    public bool IsActive { get; set; } = true;
}
