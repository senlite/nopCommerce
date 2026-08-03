namespace TwinParticles.CheckEngine.Domain.Vehicle;

public sealed class VehicleAlias
{
    public int Id { get; set; }

    public string NodeType { get; set; } = string.Empty;

    public int NodeId { get; set; }

    public string Locale { get; set; } = string.Empty;

    public string AliasText { get; set; } = string.Empty;

    public string NormalizedAlias { get; set; } = string.Empty;
}
