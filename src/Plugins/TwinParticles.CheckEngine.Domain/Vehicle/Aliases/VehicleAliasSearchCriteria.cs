namespace TwinParticles.CheckEngine.Domain.Vehicle.Aliases;

public sealed class VehicleAliasSearchCriteria
{
    public string Term { get; set; } = string.Empty;

    public string Locale { get; set; } = string.Empty;

    public int Take { get; set; } = 20;
}
