namespace TwinParticles.CheckEngine.Domain.Oem;

public sealed class Manufacturer
{
    public int Id { get; set; }

    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public bool IsOeBrand { get; set; } = true;

    public bool IsActive { get; set; } = true;
}
