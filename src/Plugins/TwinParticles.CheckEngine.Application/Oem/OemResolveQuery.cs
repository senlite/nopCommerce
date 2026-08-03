namespace TwinParticles.CheckEngine.Application.Oem;

public sealed class OemResolveQuery
{
    public string Number { get; init; } = string.Empty;

    public int? ManufacturerId { get; init; }
}
