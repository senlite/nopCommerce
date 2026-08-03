namespace TwinParticles.CheckEngine.Application.Vehicle.Aliases.Import;

public sealed class ImportVehicleAliasesResult
{
    public int TotalRows { get; set; }

    public int ImportedRows { get; set; }

    public int FailedRows { get; set; }
}
