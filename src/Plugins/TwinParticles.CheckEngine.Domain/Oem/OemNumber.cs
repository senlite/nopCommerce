namespace TwinParticles.CheckEngine.Domain.Oem;

public sealed class OemNumber
{
    public int Id { get; set; }

    public int ManufacturerId { get; set; }

    public string DisplayNumber { get; set; } = string.Empty;

    public string NormalizedNumber { get; set; } = string.Empty;

    public bool IsObsolete { get; set; }

    public bool IsActive { get; set; } = true;
}
