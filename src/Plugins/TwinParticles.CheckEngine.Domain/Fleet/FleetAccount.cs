namespace TwinParticles.CheckEngine.Domain.Fleet;

public sealed class FleetAccount
{
    public int Id { get; set; }

    public int CustomerId { get; set; }

    public string DisplayName { get; set; } = string.Empty;

    public int? DefaultBudgetCentreId { get; set; }

    public bool IsActive { get; set; } = true;
}
