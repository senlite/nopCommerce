namespace TwinParticles.CheckEngine.Domain.Vehicle.Aliases;

public sealed class VehicleAliasSearchItem
{
    public string NodeType { get; set; } = string.Empty;

    public int NodeId { get; set; }

    public string Locale { get; set; } = string.Empty;

    public string AliasText { get; set; } = string.Empty;
}
