namespace TwinParticles.CheckEngine.Domain.Vehicle;

public sealed class VehicleBody
{
    public int Id { get; set; }

    public int GenerationId { get; set; }

    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public int Doors { get; set; }

    public bool IsActive { get; set; } = true;
}
