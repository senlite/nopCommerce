using System.Collections.Generic;
using TwinParticles.CheckEngine.Domain.Dealer;
using TwinParticles.CheckEngine.Domain.Fleet;
using TwinParticles.CheckEngine.Domain.Workshop;

namespace TwinParticles.CheckEngine.Application.Portals;

public sealed class WorkshopPortalSnapshot
{
    public WorkshopAccount Account { get; init; } = new();

    public IReadOnlyList<WorkshopJob> Jobs { get; init; } = [];
}

public sealed class WorkshopJobDetail
{
    public WorkshopJob Job { get; init; } = new();

    public IReadOnlyList<WorkshopJobVehicle> Vehicles { get; init; } = [];

    public IReadOnlyList<WorkshopJobLine> Lines { get; init; } = [];
}

public sealed class FleetPortalSnapshot
{
    public FleetAccount Account { get; init; } = new();

    public IReadOnlyList<FleetVehicle> Vehicles { get; init; } = [];

    public IReadOnlyList<FleetBudgetCentre> BudgetCentres { get; init; } = [];

    public IReadOnlyList<FleetApprovalRequest> ApprovalRequests { get; init; } = [];

    public IReadOnlyList<FleetMaintenanceForecast> MaintenanceForecasts { get; init; } = [];
}

public sealed class DealerPortalSnapshot
{
    public DealerAccount Account { get; init; } = new();

    public IReadOnlyList<DealerCatalogItem> Catalog { get; init; } = [];

    public DealerQuota? Quota { get; init; }

    public IReadOnlyList<WarrantyClaim> WarrantyClaims { get; init; } = [];
}
