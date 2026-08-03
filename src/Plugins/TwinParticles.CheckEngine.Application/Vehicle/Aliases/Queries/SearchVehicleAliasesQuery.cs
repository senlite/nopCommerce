namespace TwinParticles.CheckEngine.Application.Vehicle.Aliases.Queries;

public sealed class SearchVehicleAliasesQuery
{
    public string Term { get; set; } = string.Empty;

    public string Locale { get; set; } = string.Empty;

    public int Take { get; set; } = 20;
}
