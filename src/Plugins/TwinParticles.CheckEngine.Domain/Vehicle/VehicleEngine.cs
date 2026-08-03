namespace TwinParticles.CheckEngine.Domain.Vehicle;

public sealed class VehicleEngine
{
    public int Id { get; set; }

    public int BodyId { get; set; }

    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string FuelType { get; set; } = string.Empty;

    public int DisplacementCc { get; set; }

    public int PowerHp { get; set; }

    public bool IsActive { get; set; } = true;
}
