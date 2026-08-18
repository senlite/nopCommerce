namespace TwinParticles.CheckEngine.Domain.Fleet;

public sealed class FleetMaintenanceSchedule
{
    public int Id { get; set; }

    public int FleetAccountId { get; set; }

    public string ServiceLabel { get; set; } = string.Empty;

    public int IntervalDays { get; set; }
}
