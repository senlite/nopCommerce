using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace TwinParticles.CheckEngine.Domain.Fleet;

public interface IFleetPortalRepository
{
    Task<FleetAccount?> GetAccountByCustomerIdAsync(int customerId, CancellationToken cancellationToken);

    Task<FleetAccount?> GetAccountByIdAsync(int accountId, CancellationToken cancellationToken);

    Task<int> InsertVehicleAsync(FleetVehicle vehicle, CancellationToken cancellationToken);

    Task<FleetVehicle?> GetVehicleAsync(int vehicleId, CancellationToken cancellationToken);

    Task<IReadOnlyList<FleetVehicle>> GetVehiclesAsync(int fleetAccountId, CancellationToken cancellationToken);

    Task<int> InsertImportBatchAsync(FleetVinImportBatch batch, CancellationToken cancellationToken);

    Task<FleetBudgetCentre?> GetBudgetCentreAsync(int budgetCentreId, CancellationToken cancellationToken);

    Task UpdateBudgetCentreAsync(FleetBudgetCentre centre, CancellationToken cancellationToken);

    Task<int> InsertApprovalRequestAsync(FleetApprovalRequest request, CancellationToken cancellationToken);

    Task UpdateApprovalRequestAsync(FleetApprovalRequest request, CancellationToken cancellationToken);

    Task<FleetApprovalRequest?> GetApprovalRequestAsync(int requestId, CancellationToken cancellationToken);

    Task<IReadOnlyList<FleetApprovalRequest>> ListApprovalRequestsByAccountAsync(int fleetAccountId, CancellationToken cancellationToken);

    Task<IReadOnlyList<FleetBudgetCentre>> ListBudgetCentresByAccountAsync(int fleetAccountId, CancellationToken cancellationToken);

    Task<int> InsertAccountAsync(FleetAccount account, CancellationToken cancellationToken);

    Task<int> InsertBudgetCentreAsync(FleetBudgetCentre centre, CancellationToken cancellationToken);

    Task<IReadOnlyList<FleetMaintenanceSchedule>> ListMaintenanceSchedulesAsync(int fleetAccountId, CancellationToken cancellationToken);

    Task<int> InsertMaintenanceScheduleAsync(FleetMaintenanceSchedule schedule, CancellationToken cancellationToken);

    Task<IReadOnlyList<FleetMaintenanceForecast>> ListMaintenanceForecastsAsync(int fleetAccountId, CancellationToken cancellationToken);

    Task UpsertMaintenanceForecastAsync(FleetMaintenanceForecast forecast, CancellationToken cancellationToken);

    Task<int> InsertVehicleSpendAsync(FleetVehicleSpend spend, CancellationToken cancellationToken);

    Task<IReadOnlyList<FleetVehicleCostSummary>> ListVehicleCostSummariesAsync(int fleetAccountId, CancellationToken cancellationToken);

    Task<IReadOnlyList<FleetVinImportBatch>> ListImportBatchesAsync(int fleetAccountId, CancellationToken cancellationToken);

    Task<FleetVinImportBatchDetail?> GetImportBatchDetailAsync(int batchId, CancellationToken cancellationToken);

    Task<int> InsertMemberAsync(FleetMember member, CancellationToken cancellationToken);

    Task<FleetMember?> GetMemberAsync(int fleetAccountId, int customerId, CancellationToken cancellationToken);
}
