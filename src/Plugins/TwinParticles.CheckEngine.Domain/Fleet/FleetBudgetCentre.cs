namespace TwinParticles.CheckEngine.Domain.Fleet;

public sealed class FleetBudgetCentre
{
    public int Id { get; set; }

    public int FleetAccountId { get; set; }

    public string Name { get; set; } = string.Empty;

    public decimal SpendLimit { get; set; }

    public decimal SpendUsed { get; set; }
}
