namespace TwinParticles.CheckEngine.Domain.Vehicle;

public sealed class VehicleGeneration
{
    public int Id { get; set; }

    public int ModelId { get; set; }

    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public int StartYear { get; set; }

    public int? EndYear { get; set; }

    public bool IsActive { get; set; } = true;
}
