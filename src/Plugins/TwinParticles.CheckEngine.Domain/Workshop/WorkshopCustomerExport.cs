using System;
using System.Collections.Generic;

namespace TwinParticles.CheckEngine.Domain.Workshop;

public sealed class WorkshopCustomerExport
{
    public WorkshopCustomer Customer { get; set; } = new();

    public IReadOnlyList<WorkshopCustomerVehicleExport> Vehicles { get; set; } = [];

    public DateTimeOffset ExportedUtc { get; set; }
}

public sealed class WorkshopCustomerVehicleExport
{
    public WorkshopCustomerVehicle Vehicle { get; set; } = new();

    public IReadOnlyList<WorkshopServiceHistoryEntry> ServiceHistory { get; set; } = [];
}
