using System;

namespace TwinParticles.CheckEngine.Domain.Fleet;

public sealed class FleetMaintenanceForecast
{
    public int Id { get; set; }

    public int FleetVehicleId { get; set; }

    public int ScheduleId { get; set; }

    public string ServiceLabel { get; set; } = string.Empty;

    public DateTimeOffset DueUtc { get; set; }

    public DateTimeOffset ComputedUtc { get; set; }
}
