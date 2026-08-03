namespace TwinParticles.CheckEngine.Application.Vehicle.Aliases.Commands;

public sealed class UpsertVehicleAliasCommand
{
    public string NodeType { get; set; } = string.Empty;

    public int NodeId { get; set; }

    public string Locale { get; set; } = string.Empty;

    public string AliasText { get; set; } = string.Empty;
}
