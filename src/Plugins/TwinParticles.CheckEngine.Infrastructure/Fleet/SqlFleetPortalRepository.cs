using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LinqToDB.Data;
using Nop.Data;
using TwinParticles.CheckEngine.Domain.Fleet;
using TwinParticles.CheckEngine.Infrastructure.Data;

namespace TwinParticles.CheckEngine.Infrastructure.Fleet;

public sealed class SqlFleetPortalRepository : IFleetPortalRepository
{
    private readonly INopDataProvider _dataProvider;

    public SqlFleetPortalRepository(INopDataProvider dataProvider)
    {
        _dataProvider = dataProvider;
    }

    public async Task<FleetAccount?> GetAccountByCustomerIdAsync(int customerId, CancellationToken cancellationToken)
    {
        var sql = CheckEngineSql.SelectTop(1,
            "Id, CustomerId, DisplayName, DefaultBudgetCentreId, IsActive",
            "FROM TP_CE_FleetAccount WHERE CustomerId = @customerId ORDER BY Id");
        var rows = await _dataProvider.QueryAsync<AccountRow>(sql, new DataParameter("customerId", customerId));
        return rows.Select(MapAccount).FirstOrDefault();
    }

    public async Task<FleetAccount?> GetAccountByIdAsync(int accountId, CancellationToken cancellationToken)
    {
        var rows = await _dataProvider.QueryAsync<AccountRow>(@"
SELECT Id, CustomerId, DisplayName, DefaultBudgetCentreId, IsActive
FROM TP_CE_FleetAccount
WHERE Id = @id",
            new DataParameter("id", accountId));

        return rows.Select(MapAccount).FirstOrDefault();
    }

    public async Task<int> InsertVehicleAsync(FleetVehicle vehicle, CancellationToken cancellationToken)
    {
        var id = await _dataProvider.QueryAsync<int>(@"
INSERT INTO TP_CE_FleetVehicle
(FleetAccountId, VehicleConfigurationId, Vin, AssetTag, RegisteredUtc)
VALUES
(@fleetAccountId, @vehicleConfigurationId, @vin, @assetTag, @registeredUtc);
" + CheckEngineSql.SelectInsertedIntId() + ";",
            new DataParameter("fleetAccountId", vehicle.FleetAccountId),
            new DataParameter("vehicleConfigurationId", vehicle.VehicleConfigurationId ?? (object)DBNull.Value),
            new DataParameter("vin", vehicle.Vin ?? (object)DBNull.Value),
            new DataParameter("assetTag", vehicle.AssetTag ?? (object)DBNull.Value),
            new DataParameter("registeredUtc", vehicle.RegisteredUtc?.UtcDateTime ?? (object)DBNull.Value));

        return id.FirstOrDefault();
    }

    public async Task<FleetVehicle?> GetVehicleAsync(int vehicleId, CancellationToken cancellationToken)
    {
        var rows = await _dataProvider.QueryAsync<VehicleRow>(@"
SELECT Id, FleetAccountId, VehicleConfigurationId, Vin, AssetTag, RegisteredUtc
FROM TP_CE_FleetVehicle
WHERE Id = @id",
            new DataParameter("id", vehicleId));

        return rows.Select(MapVehicle).FirstOrDefault();
    }

    public async Task<IReadOnlyList<FleetVehicle>> GetVehiclesAsync(int fleetAccountId, CancellationToken cancellationToken)
    {
        var rows = await _dataProvider.QueryAsync<VehicleRow>(@"
SELECT Id, FleetAccountId, VehicleConfigurationId, Vin, AssetTag, RegisteredUtc
FROM TP_CE_FleetVehicle
WHERE FleetAccountId = @fleetAccountId
ORDER BY Id",
            new DataParameter("fleetAccountId", fleetAccountId));

        return rows.Select(MapVehicle).ToList();
    }

    public async Task<int> InsertImportBatchAsync(FleetVinImportBatch batch, CancellationToken cancellationToken)
    {
        var batchId = 0;
        await CheckEngineSql.ExecuteInTransactionAsync(async connection =>
        {
            batchId = await CheckEngineSql.QueryScalarAsync<int>(connection, @"
INSERT INTO TP_CE_FleetVinImportBatch
(FleetAccountId, TotalRows, SucceededRows, CreatedUtc)
VALUES
(@fleetAccountId, @totalRows, @succeededRows, @createdUtc);
" + CheckEngineSql.SelectInsertedIntId(),
                new DataParameter("fleetAccountId", batch.FleetAccountId),
                new DataParameter("totalRows", batch.TotalRows),
                new DataParameter("succeededRows", batch.SucceededRows),
                new DataParameter("createdUtc", batch.CreatedUtc.UtcDateTime));

            foreach (var row in batch.Rows)
            {
                await CheckEngineSql.ExecuteAsync(connection, @"
INSERT INTO TP_CE_FleetVinImportRow
(BatchId, Vin, Outcome, VehicleConfigurationId, ReasonCode)
VALUES
(@batchId, @vin, @outcome, @vehicleConfigurationId, @reasonCode)",
                    new DataParameter("batchId", batchId),
                    new DataParameter("vin", row.Vin),
                    new DataParameter("outcome", row.Outcome),
                    new DataParameter("vehicleConfigurationId", row.VehicleConfigurationId ?? (object)DBNull.Value),
                    new DataParameter("reasonCode", row.ReasonCode ?? (object)DBNull.Value));
            }
        }, cancellationToken);

        return batchId;
    }

    public async Task<FleetBudgetCentre?> GetBudgetCentreAsync(int budgetCentreId, CancellationToken cancellationToken)
    {
        var rows = await _dataProvider.QueryAsync<BudgetCentreRow>(@"
SELECT Id, FleetAccountId, Name, SpendLimit, SpendUsed
FROM TP_CE_FleetBudgetCentre
WHERE Id = @id",
            new DataParameter("id", budgetCentreId));

        return rows.Select(MapBudgetCentre).FirstOrDefault();
    }

    public Task UpdateBudgetCentreAsync(FleetBudgetCentre centre, CancellationToken cancellationToken)
        => _dataProvider.ExecuteNonQueryAsync(@"
UPDATE TP_CE_FleetBudgetCentre
SET FleetAccountId = @fleetAccountId,
    Name = @name,
    SpendLimit = @spendLimit,
    SpendUsed = @spendUsed
WHERE Id = @id",
            new DataParameter("id", centre.Id),
            new DataParameter("fleetAccountId", centre.FleetAccountId),
            new DataParameter("name", centre.Name),
            new DataParameter("spendLimit", centre.SpendLimit),
            new DataParameter("spendUsed", centre.SpendUsed));

    public async Task<int> InsertApprovalRequestAsync(FleetApprovalRequest request, CancellationToken cancellationToken)
    {
        var id = await _dataProvider.QueryAsync<int>(@"
INSERT INTO TP_CE_FleetApprovalRequest
(FleetAccountId, FleetVehicleId, ProductId, Quantity, BudgetCentreId, RequesterCustomerId, StatusId, RejectionReason, OrderId, CreatedUtc, UpdatedUtc)
VALUES
(@fleetAccountId, @fleetVehicleId, @productId, @quantity, @budgetCentreId, @requesterCustomerId, @statusId, @rejectionReason, @orderId, @createdUtc, @updatedUtc);
" + CheckEngineSql.SelectInsertedIntId() + ";",
            new DataParameter("fleetAccountId", request.FleetAccountId),
            new DataParameter("fleetVehicleId", request.FleetVehicleId),
            new DataParameter("productId", request.ProductId),
            new DataParameter("quantity", request.Quantity),
            new DataParameter("budgetCentreId", request.BudgetCentreId),
            new DataParameter("requesterCustomerId", request.RequesterCustomerId),
            new DataParameter("statusId", (int)request.Status),
            new DataParameter("rejectionReason", request.RejectionReason ?? (object)DBNull.Value),
            new DataParameter("orderId", request.OrderId ?? (object)DBNull.Value),
            new DataParameter("createdUtc", request.CreatedUtc.UtcDateTime),
            new DataParameter("updatedUtc", request.UpdatedUtc.UtcDateTime));

        return id.FirstOrDefault();
    }

    public Task UpdateApprovalRequestAsync(FleetApprovalRequest request, CancellationToken cancellationToken)
        => _dataProvider.ExecuteNonQueryAsync(@"
UPDATE TP_CE_FleetApprovalRequest
SET FleetAccountId = @fleetAccountId,
    FleetVehicleId = @fleetVehicleId,
    ProductId = @productId,
    Quantity = @quantity,
    BudgetCentreId = @budgetCentreId,
    RequesterCustomerId = @requesterCustomerId,
    StatusId = @statusId,
    RejectionReason = @rejectionReason,
    OrderId = @orderId,
    UpdatedUtc = @updatedUtc
WHERE Id = @id",
            new DataParameter("id", request.Id),
            new DataParameter("fleetAccountId", request.FleetAccountId),
            new DataParameter("fleetVehicleId", request.FleetVehicleId),
            new DataParameter("productId", request.ProductId),
            new DataParameter("quantity", request.Quantity),
            new DataParameter("budgetCentreId", request.BudgetCentreId),
            new DataParameter("requesterCustomerId", request.RequesterCustomerId),
            new DataParameter("statusId", (int)request.Status),
            new DataParameter("rejectionReason", request.RejectionReason ?? (object)DBNull.Value),
            new DataParameter("orderId", request.OrderId ?? (object)DBNull.Value),
            new DataParameter("updatedUtc", request.UpdatedUtc.UtcDateTime));

    public async Task<FleetApprovalRequest?> GetApprovalRequestAsync(int requestId, CancellationToken cancellationToken)
    {
        var rows = await _dataProvider.QueryAsync<ApprovalRequestRow>(@"
SELECT Id, FleetAccountId, FleetVehicleId, ProductId, Quantity, BudgetCentreId, RequesterCustomerId,
       StatusId, RejectionReason, OrderId, CreatedUtc, UpdatedUtc
FROM TP_CE_FleetApprovalRequest
WHERE Id = @id",
            new DataParameter("id", requestId));

        return rows.Select(MapApprovalRequest).FirstOrDefault();
    }

    public async Task<IReadOnlyList<FleetApprovalRequest>> ListApprovalRequestsByAccountAsync(int fleetAccountId, CancellationToken cancellationToken)
    {
        var rows = await _dataProvider.QueryAsync<ApprovalRequestRow>(@"
SELECT Id, FleetAccountId, FleetVehicleId, ProductId, Quantity, BudgetCentreId, RequesterCustomerId,
       StatusId, RejectionReason, OrderId, CreatedUtc, UpdatedUtc
FROM TP_CE_FleetApprovalRequest
WHERE FleetAccountId = @accountId
ORDER BY UpdatedUtc DESC",
            new DataParameter("accountId", fleetAccountId));

        return rows.Select(MapApprovalRequest).ToList();
    }

    public async Task<IReadOnlyList<FleetBudgetCentre>> ListBudgetCentresByAccountAsync(int fleetAccountId, CancellationToken cancellationToken)
    {
        var rows = await _dataProvider.QueryAsync<BudgetCentreRow>(@"
SELECT Id, FleetAccountId, Name, SpendLimit, SpendUsed
FROM TP_CE_FleetBudgetCentre
WHERE FleetAccountId = @accountId
ORDER BY Name",
            new DataParameter("accountId", fleetAccountId));

        return rows.Select(MapBudgetCentre).ToList();
    }

    public async Task<int> InsertAccountAsync(FleetAccount account, CancellationToken cancellationToken)
    {
        var id = await _dataProvider.QueryAsync<int>(@"
INSERT INTO TP_CE_FleetAccount
(CustomerId, DisplayName, DefaultBudgetCentreId, IsActive)
VALUES
(@customerId, @displayName, @defaultBudgetCentreId, @isActive);
" + CheckEngineSql.SelectInsertedIntId() + ";",
            new DataParameter("customerId", account.CustomerId),
            new DataParameter("displayName", account.DisplayName),
            new DataParameter("defaultBudgetCentreId", account.DefaultBudgetCentreId ?? (object)DBNull.Value),
            new DataParameter("isActive", account.IsActive));

        return id.FirstOrDefault();
    }

    public async Task<int> InsertBudgetCentreAsync(FleetBudgetCentre centre, CancellationToken cancellationToken)
    {
        var id = await _dataProvider.QueryAsync<int>(@"
INSERT INTO TP_CE_FleetBudgetCentre
(FleetAccountId, Name, SpendLimit, SpendUsed)
VALUES
(@fleetAccountId, @name, @spendLimit, @spendUsed);
" + CheckEngineSql.SelectInsertedIntId() + ";",
            new DataParameter("fleetAccountId", centre.FleetAccountId),
            new DataParameter("name", centre.Name),
            new DataParameter("spendLimit", centre.SpendLimit),
            new DataParameter("spendUsed", centre.SpendUsed));

        return id.FirstOrDefault();
    }

    public async Task<IReadOnlyList<FleetMaintenanceSchedule>> ListMaintenanceSchedulesAsync(int fleetAccountId, CancellationToken cancellationToken)
    {
        var rows = await _dataProvider.QueryAsync<MaintenanceScheduleRow>(@"
SELECT Id, FleetAccountId, ServiceLabel, IntervalDays
FROM TP_CE_FleetMaintenanceSchedule
WHERE FleetAccountId = @fleetAccountId
ORDER BY IntervalDays",
            new DataParameter("fleetAccountId", fleetAccountId));

        return rows.Select(MapMaintenanceSchedule).ToList();
    }

    public async Task<int> InsertMaintenanceScheduleAsync(FleetMaintenanceSchedule schedule, CancellationToken cancellationToken)
    {
        var id = await _dataProvider.QueryAsync<int>(@"
INSERT INTO TP_CE_FleetMaintenanceSchedule
(FleetAccountId, ServiceLabel, IntervalDays)
VALUES
(@fleetAccountId, @serviceLabel, @intervalDays);
" + CheckEngineSql.SelectInsertedIntId() + ";",
            new DataParameter("fleetAccountId", schedule.FleetAccountId),
            new DataParameter("serviceLabel", schedule.ServiceLabel),
            new DataParameter("intervalDays", schedule.IntervalDays));

        return id.FirstOrDefault();
    }

    public async Task<IReadOnlyList<FleetMaintenanceForecast>> ListMaintenanceForecastsAsync(int fleetAccountId, CancellationToken cancellationToken)
    {
        var rows = await _dataProvider.QueryAsync<MaintenanceForecastRow>(@"
SELECT f.Id, f.FleetVehicleId, f.ScheduleId, f.ServiceLabel, f.DueUtc, f.ComputedUtc
FROM TP_CE_FleetMaintenanceForecast f
INNER JOIN TP_CE_FleetVehicle v ON v.Id = f.FleetVehicleId
WHERE v.FleetAccountId = @fleetAccountId
ORDER BY f.DueUtc",
            new DataParameter("fleetAccountId", fleetAccountId));

        return rows.Select(MapMaintenanceForecast).ToList();
    }

    public async Task UpsertMaintenanceForecastAsync(FleetMaintenanceForecast forecast, CancellationToken cancellationToken)
    {
        var existing = await _dataProvider.QueryAsync<int>(@"
SELECT Id
FROM TP_CE_FleetMaintenanceForecast
WHERE FleetVehicleId = @fleetVehicleId AND ScheduleId = @scheduleId",
            new DataParameter("fleetVehicleId", forecast.FleetVehicleId),
            new DataParameter("scheduleId", forecast.ScheduleId));

        var existingId = existing.FirstOrDefault();
        if (existingId > 0)
        {
            await _dataProvider.ExecuteNonQueryAsync(@"
UPDATE TP_CE_FleetMaintenanceForecast
SET ServiceLabel = @serviceLabel,
    DueUtc = @dueUtc,
    ComputedUtc = @computedUtc
WHERE Id = @id",
                new DataParameter("id", existingId),
                new DataParameter("serviceLabel", forecast.ServiceLabel),
                new DataParameter("dueUtc", forecast.DueUtc.UtcDateTime),
                new DataParameter("computedUtc", forecast.ComputedUtc.UtcDateTime));
            forecast.Id = existingId;
            return;
        }

        var id = await _dataProvider.QueryAsync<int>(@"
INSERT INTO TP_CE_FleetMaintenanceForecast
(FleetVehicleId, ScheduleId, ServiceLabel, DueUtc, ComputedUtc)
VALUES
(@fleetVehicleId, @scheduleId, @serviceLabel, @dueUtc, @computedUtc);
" + CheckEngineSql.SelectInsertedIntId() + ";",
            new DataParameter("fleetVehicleId", forecast.FleetVehicleId),
            new DataParameter("scheduleId", forecast.ScheduleId),
            new DataParameter("serviceLabel", forecast.ServiceLabel),
            new DataParameter("dueUtc", forecast.DueUtc.UtcDateTime),
            new DataParameter("computedUtc", forecast.ComputedUtc.UtcDateTime));

        forecast.Id = id.FirstOrDefault();
    }

    public async Task<int> InsertVehicleSpendAsync(FleetVehicleSpend spend, CancellationToken cancellationToken)
    {
        var id = await _dataProvider.QueryAsync<int>(@"
INSERT INTO TP_CE_FleetVehicleSpend
(FleetAccountId, FleetVehicleId, ProductId, OrderId, Amount, RecordedUtc)
VALUES
(@fleetAccountId, @fleetVehicleId, @productId, @orderId, @amount, @recordedUtc);
" + CheckEngineSql.SelectInsertedIntId() + ";",
            new DataParameter("fleetAccountId", spend.FleetAccountId),
            new DataParameter("fleetVehicleId", spend.FleetVehicleId),
            new DataParameter("productId", spend.ProductId),
            new DataParameter("orderId", spend.OrderId ?? (object)DBNull.Value),
            new DataParameter("amount", spend.Amount),
            new DataParameter("recordedUtc", spend.RecordedUtc.UtcDateTime));

        return id.FirstOrDefault();
    }

    public async Task<IReadOnlyList<FleetVehicleCostSummary>> ListVehicleCostSummariesAsync(int fleetAccountId, CancellationToken cancellationToken)
    {
        var rows = await _dataProvider.QueryAsync<CostSummaryRow>(@"
SELECT s.FleetVehicleId,
       v.Vin,
       SUM(s.Amount) AS TotalSpend,
       COUNT(DISTINCT s.OrderId) AS OrderCount
FROM TP_CE_FleetVehicleSpend s
INNER JOIN TP_CE_FleetVehicle v ON v.Id = s.FleetVehicleId
WHERE s.FleetAccountId = @fleetAccountId
GROUP BY s.FleetVehicleId, v.Vin
ORDER BY TotalSpend DESC",
            new DataParameter("fleetAccountId", fleetAccountId));

        return rows.Select(row => new FleetVehicleCostSummary
        {
            FleetVehicleId = row.FleetVehicleId,
            Vin = row.Vin,
            TotalSpend = row.TotalSpend,
            OrderCount = row.OrderCount
        }).ToList();
    }

    public async Task<IReadOnlyList<FleetVinImportBatch>> ListImportBatchesAsync(int fleetAccountId, CancellationToken cancellationToken)
    {
        var rows = await _dataProvider.QueryAsync<ImportBatchRow>(@"
SELECT Id, FleetAccountId, TotalRows, SucceededRows, CreatedUtc
FROM TP_CE_FleetVinImportBatch
WHERE FleetAccountId = @fleetAccountId
ORDER BY CreatedUtc DESC",
            new DataParameter("fleetAccountId", fleetAccountId));

        return rows.Select(MapImportBatch).ToList();
    }

    public async Task<FleetVinImportBatchDetail?> GetImportBatchDetailAsync(int batchId, CancellationToken cancellationToken)
    {
        var batchRows = await _dataProvider.QueryAsync<ImportBatchRow>(@"
SELECT Id, FleetAccountId, TotalRows, SucceededRows, CreatedUtc
FROM TP_CE_FleetVinImportBatch
WHERE Id = @id",
            new DataParameter("id", batchId));

        var batch = batchRows.Select(MapImportBatch).FirstOrDefault();
        if (batch is null)
            return null;

        var rowRows = await _dataProvider.QueryAsync<ImportRowRow>(@"
SELECT BatchId, Vin, Outcome, VehicleConfigurationId, ReasonCode
FROM TP_CE_FleetVinImportRow
WHERE BatchId = @batchId
ORDER BY Vin",
            new DataParameter("batchId", batchId));

        return new FleetVinImportBatchDetail
        {
            Batch = batch,
            Rows = rowRows.Select(row => new FleetVinImportRow
            {
                Vin = row.Vin,
                Outcome = row.Outcome,
                VehicleConfigurationId = row.VehicleConfigurationId,
                ReasonCode = row.ReasonCode
            }).ToList()
        };
    }

    public async Task<int> InsertMemberAsync(FleetMember member, CancellationToken cancellationToken)
    {
        var id = await _dataProvider.QueryAsync<int>(@"
INSERT INTO TP_CE_FleetMember (FleetAccountId, CustomerId, CanApprove)
VALUES (@fleetAccountId, @customerId, @canApprove);
" + CheckEngineSql.SelectInsertedIntId() + ";",
            new DataParameter("fleetAccountId", member.FleetAccountId),
            new DataParameter("customerId", member.CustomerId),
            new DataParameter("canApprove", member.CanApprove));

        return id.FirstOrDefault();
    }

    public async Task<FleetMember?> GetMemberAsync(int fleetAccountId, int customerId, CancellationToken cancellationToken)
    {
        var sql = CheckEngineSql.SelectTop(1,
            "Id, FleetAccountId, CustomerId, CanApprove",
            "FROM TP_CE_FleetMember WHERE FleetAccountId = @fleetAccountId AND CustomerId = @customerId ORDER BY Id");
        var rows = await _dataProvider.QueryAsync<MemberRow>(sql,
            new DataParameter("fleetAccountId", fleetAccountId),
            new DataParameter("customerId", customerId));

        return rows.Select(row => new FleetMember
        {
            Id = row.Id,
            FleetAccountId = row.FleetAccountId,
            CustomerId = row.CustomerId,
            CanApprove = row.CanApprove
        }).FirstOrDefault();
    }

    private static FleetVinImportBatch MapImportBatch(ImportBatchRow row)
    {
        return new FleetVinImportBatch
        {
            Id = row.Id,
            FleetAccountId = row.FleetAccountId,
            TotalRows = row.TotalRows,
            SucceededRows = row.SucceededRows,
            CreatedUtc = ToUtc(row.CreatedUtc)
        };
    }

    private static FleetAccount MapAccount(AccountRow row)
    {
        return new FleetAccount
        {
            Id = row.Id,
            CustomerId = row.CustomerId,
            DisplayName = row.DisplayName,
            DefaultBudgetCentreId = row.DefaultBudgetCentreId,
            IsActive = row.IsActive
        };
    }

    private static FleetVehicle MapVehicle(VehicleRow row)
    {
        return new FleetVehicle
        {
            Id = row.Id,
            FleetAccountId = row.FleetAccountId,
            VehicleConfigurationId = row.VehicleConfigurationId,
            Vin = row.Vin,
            AssetTag = row.AssetTag,
            RegisteredUtc = row.RegisteredUtc.HasValue ? ToUtc(row.RegisteredUtc.Value) : null
        };
    }

    private static FleetMaintenanceSchedule MapMaintenanceSchedule(MaintenanceScheduleRow row)
    {
        return new FleetMaintenanceSchedule
        {
            Id = row.Id,
            FleetAccountId = row.FleetAccountId,
            ServiceLabel = row.ServiceLabel,
            IntervalDays = row.IntervalDays
        };
    }

    private static FleetMaintenanceForecast MapMaintenanceForecast(MaintenanceForecastRow row)
    {
        return new FleetMaintenanceForecast
        {
            Id = row.Id,
            FleetVehicleId = row.FleetVehicleId,
            ScheduleId = row.ScheduleId,
            ServiceLabel = row.ServiceLabel,
            DueUtc = ToUtc(row.DueUtc),
            ComputedUtc = ToUtc(row.ComputedUtc)
        };
    }

    private static FleetBudgetCentre MapBudgetCentre(BudgetCentreRow row)
    {
        return new FleetBudgetCentre
        {
            Id = row.Id,
            FleetAccountId = row.FleetAccountId,
            Name = row.Name,
            SpendLimit = row.SpendLimit,
            SpendUsed = row.SpendUsed
        };
    }

    private static FleetApprovalRequest MapApprovalRequest(ApprovalRequestRow row)
    {
        return new FleetApprovalRequest
        {
            Id = row.Id,
            FleetAccountId = row.FleetAccountId,
            FleetVehicleId = row.FleetVehicleId,
            ProductId = row.ProductId,
            Quantity = row.Quantity,
            BudgetCentreId = row.BudgetCentreId,
            RequesterCustomerId = row.RequesterCustomerId,
            Status = (FleetApprovalStatus)row.StatusId,
            RejectionReason = row.RejectionReason,
            OrderId = row.OrderId,
            CreatedUtc = ToUtc(row.CreatedUtc),
            UpdatedUtc = ToUtc(row.UpdatedUtc)
        };
    }

    private static DateTimeOffset ToUtc(DateTime value)
        => new(DateTime.SpecifyKind(value, DateTimeKind.Utc));

    private sealed class AccountRow
    {
        public int Id { get; set; }
        public int CustomerId { get; set; }
        public string DisplayName { get; set; } = string.Empty;
        public int? DefaultBudgetCentreId { get; set; }
        public bool IsActive { get; set; }
    }

    private sealed class VehicleRow
    {
        public int Id { get; set; }
        public int FleetAccountId { get; set; }
        public int? VehicleConfigurationId { get; set; }
        public string? Vin { get; set; }
        public string? AssetTag { get; set; }
        public DateTime? RegisteredUtc { get; set; }
    }

    private sealed class MaintenanceScheduleRow
    {
        public int Id { get; set; }
        public int FleetAccountId { get; set; }
        public string ServiceLabel { get; set; } = string.Empty;
        public int IntervalDays { get; set; }
    }

    private sealed class MaintenanceForecastRow
    {
        public int Id { get; set; }
        public int FleetVehicleId { get; set; }
        public int ScheduleId { get; set; }
        public string ServiceLabel { get; set; } = string.Empty;
        public DateTime DueUtc { get; set; }
        public DateTime ComputedUtc { get; set; }
    }

    private sealed class CostSummaryRow
    {
        public int FleetVehicleId { get; set; }
        public string? Vin { get; set; }
        public decimal TotalSpend { get; set; }
        public int OrderCount { get; set; }
    }

    private sealed class ImportBatchRow
    {
        public int Id { get; set; }
        public int FleetAccountId { get; set; }
        public int TotalRows { get; set; }
        public int SucceededRows { get; set; }
        public DateTime CreatedUtc { get; set; }
    }

    private sealed class ImportRowRow
    {
        public int BatchId { get; set; }
        public string Vin { get; set; } = string.Empty;
        public string Outcome { get; set; } = string.Empty;
        public int? VehicleConfigurationId { get; set; }
        public string? ReasonCode { get; set; }
    }

    private sealed class BudgetCentreRow
    {
        public int Id { get; set; }
        public int FleetAccountId { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal SpendLimit { get; set; }
        public decimal SpendUsed { get; set; }
    }

    private sealed class ApprovalRequestRow
    {
        public int Id { get; set; }
        public int FleetAccountId { get; set; }
        public int FleetVehicleId { get; set; }
        public int ProductId { get; set; }
        public int Quantity { get; set; }
        public int BudgetCentreId { get; set; }
        public int RequesterCustomerId { get; set; }
        public int StatusId { get; set; }
        public string? RejectionReason { get; set; }
        public int? OrderId { get; set; }
        public DateTime CreatedUtc { get; set; }
        public DateTime UpdatedUtc { get; set; }
    }

    private sealed class MemberRow
    {
        public int Id { get; set; }
        public int FleetAccountId { get; set; }
        public int CustomerId { get; set; }
        public bool CanApprove { get; set; }
    }
}
