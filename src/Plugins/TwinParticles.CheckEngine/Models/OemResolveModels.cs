namespace TwinParticles.CheckEngine.Models;

public sealed class OemResolveRequestModel
{
    public string Number { get; set; } = string.Empty;

    public int? ManufacturerId { get; set; }
}
