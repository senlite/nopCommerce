using System;

namespace TwinParticles.CheckEngine.Domain.Fleet;

public sealed class FleetVehicleSpend
{
    public int Id { get; set; }

    public int FleetAccountId { get; set; }

    public int FleetVehicleId { get; set; }

    public int ProductId { get; set; }

    public int? OrderId { get; set; }

    public decimal Amount { get; set; }

    public DateTimeOffset RecordedUtc { get; set; }
}

public sealed class FleetVehicleCostSummary
{
    public int FleetVehicleId { get; set; }

    public string? Vin { get; set; }

    public decimal TotalSpend { get; set; }

    public int OrderCount { get; set; }
}
