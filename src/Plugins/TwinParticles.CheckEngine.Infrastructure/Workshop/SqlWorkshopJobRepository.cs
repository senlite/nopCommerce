using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LinqToDB.Data;
using Nop.Data;
using TwinParticles.CheckEngine.Domain.Workshop;
using TwinParticles.CheckEngine.Infrastructure.Data;

namespace TwinParticles.CheckEngine.Infrastructure.Workshop;

public sealed class SqlWorkshopJobRepository : IWorkshopJobRepository
{
    private readonly INopDataProvider _dataProvider;

    public SqlWorkshopJobRepository(INopDataProvider dataProvider)
    {
        _dataProvider = dataProvider;
    }

    public async Task<WorkshopAccount?> GetAccountByCustomerIdAsync(int customerId, CancellationToken cancellationToken)
    {
        var sql = CheckEngineSql.SelectTop(1,
            "Id, CustomerId, DisplayName, CreditLimit, CreditUsed, DefaultPriceListId, IsActive",
            "FROM TP_CE_WorkshopAccount WHERE CustomerId = @customerId ORDER BY Id");
        var rows = await _dataProvider.QueryAsync<AccountRow>(sql, new DataParameter("customerId", customerId));
        return rows.Select(MapAccount).FirstOrDefault();
    }

    public async Task<WorkshopAccount?> GetAccountByIdAsync(int accountId, CancellationToken cancellationToken)
    {
        var rows = await _dataProvider.QueryAsync<AccountRow>(@"
SELECT Id, CustomerId, DisplayName, CreditLimit, CreditUsed, DefaultPriceListId, IsActive
FROM TP_CE_WorkshopAccount
WHERE Id = @id",
            new DataParameter("id", accountId));

        return rows.Select(MapAccount).FirstOrDefault();
    }

    public async Task<int> InsertJobAsync(WorkshopJob job, CancellationToken cancellationToken)
    {
        var id = await _dataProvider.QueryAsync<int>(@"
INSERT INTO TP_CE_WorkshopJob
(WorkshopAccountId, WorkshopCustomerId, AssignedTechnicianCustomerId, StatusId, LabourEstimate, OrderId, CreatedUtc, UpdatedUtc)
VALUES
(@workshopAccountId, @workshopCustomerId, @assignedTechnicianCustomerId, @statusId, @labourEstimate, @orderId, @createdUtc, @updatedUtc);
" + CheckEngineSql.SelectInsertedIntId() + ";",
            new DataParameter("workshopAccountId", job.WorkshopAccountId),
            new DataParameter("workshopCustomerId", job.WorkshopCustomerId ?? (object)DBNull.Value),
            new DataParameter("assignedTechnicianCustomerId", job.AssignedTechnicianCustomerId ?? (object)DBNull.Value),
            new DataParameter("statusId", (int)job.Status),
            new DataParameter("labourEstimate", job.LabourEstimate),
            new DataParameter("orderId", job.OrderId ?? (object)DBNull.Value),
            new DataParameter("createdUtc", job.CreatedUtc.UtcDateTime),
            new DataParameter("updatedUtc", job.UpdatedUtc.UtcDateTime));

        return id.FirstOrDefault();
    }

    public Task UpdateJobAsync(WorkshopJob job, CancellationToken cancellationToken)
        => _dataProvider.ExecuteNonQueryAsync(@"
UPDATE TP_CE_WorkshopJob
SET WorkshopAccountId = @workshopAccountId,
    WorkshopCustomerId = @workshopCustomerId,
    AssignedTechnicianCustomerId = @assignedTechnicianCustomerId,
    StatusId = @statusId,
    LabourEstimate = @labourEstimate,
    OrderId = @orderId,
    UpdatedUtc = @updatedUtc
WHERE Id = @id",
            new DataParameter("id", job.Id),
            new DataParameter("workshopAccountId", job.WorkshopAccountId),
            new DataParameter("workshopCustomerId", job.WorkshopCustomerId ?? (object)DBNull.Value),
            new DataParameter("assignedTechnicianCustomerId", job.AssignedTechnicianCustomerId ?? (object)DBNull.Value),
            new DataParameter("statusId", (int)job.Status),
            new DataParameter("labourEstimate", job.LabourEstimate),
            new DataParameter("orderId", job.OrderId ?? (object)DBNull.Value),
            new DataParameter("updatedUtc", job.UpdatedUtc.UtcDateTime));

    public async Task<WorkshopJob?> GetJobAsync(int jobId, CancellationToken cancellationToken)
    {
        var rows = await _dataProvider.QueryAsync<JobRow>(@"
SELECT Id, WorkshopAccountId, WorkshopCustomerId, AssignedTechnicianCustomerId, StatusId, LabourEstimate, OrderId, CreatedUtc, UpdatedUtc
FROM TP_CE_WorkshopJob
WHERE Id = @id",
            new DataParameter("id", jobId));

        return rows.Select(MapJob).FirstOrDefault();
    }

    public async Task<int> InsertJobVehicleAsync(WorkshopJobVehicle vehicle, CancellationToken cancellationToken)
    {
        var id = await _dataProvider.QueryAsync<int>(@"
INSERT INTO TP_CE_WorkshopJobVehicle
(JobId, VehicleConfigurationId, Vin, Label)
VALUES
(@jobId, @vehicleConfigurationId, @vin, @label);
" + CheckEngineSql.SelectInsertedIntId() + ";",
            new DataParameter("jobId", vehicle.JobId),
            new DataParameter("vehicleConfigurationId", vehicle.VehicleConfigurationId),
            new DataParameter("vin", vehicle.Vin ?? (object)DBNull.Value),
            new DataParameter("label", vehicle.Label ?? (object)DBNull.Value));

        return id.FirstOrDefault();
    }

    public async Task<IReadOnlyList<WorkshopJobVehicle>> GetJobVehiclesAsync(int jobId, CancellationToken cancellationToken)
    {
        var rows = await _dataProvider.QueryAsync<JobVehicleRow>(@"
SELECT Id, JobId, VehicleConfigurationId, Vin, Label
FROM TP_CE_WorkshopJobVehicle
WHERE JobId = @jobId
ORDER BY Id",
            new DataParameter("jobId", jobId));

        return rows.Select(MapJobVehicle).ToList();
    }

    public async Task<WorkshopJobVehicle?> GetJobVehicleAsync(int jobVehicleId, CancellationToken cancellationToken)
    {
        var rows = await _dataProvider.QueryAsync<JobVehicleRow>(@"
SELECT Id, JobId, VehicleConfigurationId, Vin, Label
FROM TP_CE_WorkshopJobVehicle
WHERE Id = @id",
            new DataParameter("id", jobVehicleId));

        return rows.Select(MapJobVehicle).FirstOrDefault();
    }

    public async Task<int> InsertJobLineAsync(WorkshopJobLine line, CancellationToken cancellationToken)
    {
        var id = await _dataProvider.QueryAsync<int>(@"
INSERT INTO TP_CE_WorkshopJobLine
(JobId, JobVehicleId, ProductId, Quantity, FitmentOutcome, UnitPrice)
VALUES
(@jobId, @jobVehicleId, @productId, @quantity, @fitmentOutcome, @unitPrice);
" + CheckEngineSql.SelectInsertedIntId() + ";",
            new DataParameter("jobId", line.JobId),
            new DataParameter("jobVehicleId", line.JobVehicleId),
            new DataParameter("productId", line.ProductId),
            new DataParameter("quantity", line.Quantity),
            new DataParameter("fitmentOutcome", line.FitmentOutcome),
            new DataParameter("unitPrice", line.UnitPrice));

        return id.FirstOrDefault();
    }

    public async Task<IReadOnlyList<WorkshopJobLine>> GetJobLinesAsync(int jobId, CancellationToken cancellationToken)
    {
        var rows = await _dataProvider.QueryAsync<JobLineRow>(@"
SELECT Id, JobId, JobVehicleId, ProductId, Quantity, FitmentOutcome, UnitPrice, InvoicedOrderId
FROM TP_CE_WorkshopJobLine
WHERE JobId = @jobId
ORDER BY Id",
            new DataParameter("jobId", jobId));

        return rows.Select(MapJobLine).ToList();
    }

    public async Task<IReadOnlyList<WorkshopJob>> ListJobsByAccountAsync(int workshopAccountId, CancellationToken cancellationToken)
    {
        var rows = await _dataProvider.QueryAsync<JobRow>(@"
SELECT Id, WorkshopAccountId, WorkshopCustomerId, AssignedTechnicianCustomerId, StatusId, LabourEstimate, OrderId, CreatedUtc, UpdatedUtc
FROM TP_CE_WorkshopJob
WHERE WorkshopAccountId = @accountId
ORDER BY UpdatedUtc DESC",
            new DataParameter("accountId", workshopAccountId));

        return rows.Select(MapJob).ToList();
    }

    public async Task<int> InsertAccountAsync(WorkshopAccount account, CancellationToken cancellationToken)
    {
        var id = await _dataProvider.QueryAsync<int>(@"
INSERT INTO TP_CE_WorkshopAccount
(CustomerId, DisplayName, CreditLimit, CreditUsed, DefaultPriceListId, IsActive)
VALUES
(@customerId, @displayName, @creditLimit, @creditUsed, @defaultPriceListId, @isActive);
" + CheckEngineSql.SelectInsertedIntId() + ";",
            new DataParameter("customerId", account.CustomerId),
            new DataParameter("displayName", account.DisplayName),
            new DataParameter("creditLimit", account.CreditLimit),
            new DataParameter("creditUsed", account.CreditUsed),
            new DataParameter("defaultPriceListId", account.DefaultPriceListId ?? (object)DBNull.Value),
            new DataParameter("isActive", account.IsActive));

        return id.FirstOrDefault();
    }

    public Task UpdateAccountAsync(WorkshopAccount account, CancellationToken cancellationToken)
        => _dataProvider.ExecuteNonQueryAsync(@"
UPDATE TP_CE_WorkshopAccount
SET CustomerId = @customerId,
    DisplayName = @displayName,
    CreditLimit = @creditLimit,
    CreditUsed = @creditUsed,
    DefaultPriceListId = @defaultPriceListId,
    IsActive = @isActive
WHERE Id = @id",
            new DataParameter("id", account.Id),
            new DataParameter("customerId", account.CustomerId),
            new DataParameter("displayName", account.DisplayName),
            new DataParameter("creditLimit", account.CreditLimit),
            new DataParameter("creditUsed", account.CreditUsed),
            new DataParameter("defaultPriceListId", account.DefaultPriceListId ?? (object)DBNull.Value),
            new DataParameter("isActive", account.IsActive));

    public async Task<int> InsertPriceListAsync(string name, CancellationToken cancellationToken)
    {
        var id = await _dataProvider.QueryAsync<int>(@"
INSERT INTO TP_CE_PriceList (Name, IsActive) VALUES (@name, 1);
" + CheckEngineSql.SelectInsertedIntId() + ";",
            new DataParameter("name", name));

        return id.FirstOrDefault();
    }

    public Task InsertPriceListItemAsync(int priceListId, int productId, decimal unitPrice, CancellationToken cancellationToken)
        => _dataProvider.ExecuteNonQueryAsync(@"
INSERT INTO TP_CE_PriceListItem (PriceListId, ProductId, UnitPrice)
VALUES (@priceListId, @productId, @unitPrice)",
            new DataParameter("priceListId", priceListId),
            new DataParameter("productId", productId),
            new DataParameter("unitPrice", unitPrice));

    public async Task<decimal> ResolveTradePriceAsync(int priceListId, int productId, CancellationToken cancellationToken)
    {
        var sql = CheckEngineSql.SelectTop(1,
            "UnitPrice",
            "FROM TP_CE_PriceListItem WHERE PriceListId = @priceListId AND ProductId = @productId ORDER BY Id");
        var rows = await _dataProvider.QueryAsync<decimal>(sql,
            new DataParameter("priceListId", priceListId),
            new DataParameter("productId", productId));

        return rows.FirstOrDefault();
    }

    public async Task<int> InsertCustomerAsync(WorkshopCustomer customer, CancellationToken cancellationToken)
    {
        var id = await _dataProvider.QueryAsync<int>(@"
INSERT INTO TP_CE_WorkshopCustomer (WorkshopAccountId, DisplayName, ContactEmail)
VALUES (@workshopAccountId, @displayName, @contactEmail);
" + CheckEngineSql.SelectInsertedIntId() + ";",
            new DataParameter("workshopAccountId", customer.WorkshopAccountId),
            new DataParameter("displayName", customer.DisplayName),
            new DataParameter("contactEmail", customer.ContactEmail ?? (object)DBNull.Value));

        return id.FirstOrDefault();
    }

    public async Task<int> InsertCustomerVehicleAsync(WorkshopCustomerVehicle vehicle, CancellationToken cancellationToken)
    {
        var id = await _dataProvider.QueryAsync<int>(@"
INSERT INTO TP_CE_WorkshopCustomerVehicle (WorkshopCustomerId, VehicleConfigurationId, Vin)
VALUES (@workshopCustomerId, @vehicleConfigurationId, @vin);
" + CheckEngineSql.SelectInsertedIntId() + ";",
            new DataParameter("workshopCustomerId", vehicle.WorkshopCustomerId),
            new DataParameter("vehicleConfigurationId", vehicle.VehicleConfigurationId),
            new DataParameter("vin", vehicle.Vin ?? (object)DBNull.Value));

        return id.FirstOrDefault();
    }

    public async Task<IReadOnlyList<WorkshopCustomer>> ListCustomersAsync(int workshopAccountId, CancellationToken cancellationToken)
    {
        var rows = await _dataProvider.QueryAsync<CustomerRow>(@"
SELECT Id, WorkshopAccountId, DisplayName, ContactEmail
FROM TP_CE_WorkshopCustomer
WHERE WorkshopAccountId = @workshopAccountId
ORDER BY DisplayName",
            new DataParameter("workshopAccountId", workshopAccountId));

        return rows.Select(row => new WorkshopCustomer
        {
            Id = row.Id,
            WorkshopAccountId = row.WorkshopAccountId,
            DisplayName = row.DisplayName,
            ContactEmail = row.ContactEmail
        }).ToList();
    }

    public async Task<IReadOnlyList<WorkshopCustomerVehicle>> ListCustomerVehiclesAsync(int workshopCustomerId, CancellationToken cancellationToken)
    {
        var rows = await _dataProvider.QueryAsync<CustomerVehicleRow>(@"
SELECT Id, WorkshopCustomerId, VehicleConfigurationId, Vin
FROM TP_CE_WorkshopCustomerVehicle
WHERE WorkshopCustomerId = @workshopCustomerId
ORDER BY Id",
            new DataParameter("workshopCustomerId", workshopCustomerId));

        return rows.Select(row => new WorkshopCustomerVehicle
        {
            Id = row.Id,
            WorkshopCustomerId = row.WorkshopCustomerId,
            VehicleConfigurationId = row.VehicleConfigurationId,
            Vin = row.Vin
        }).ToList();
    }

    public async Task<int> InsertTechnicianAsync(WorkshopTechnician technician, CancellationToken cancellationToken)
    {
        var id = await _dataProvider.QueryAsync<int>(@"
INSERT INTO TP_CE_WorkshopTechnician (WorkshopAccountId, CustomerId, CanRaiseInvoice, IsFrontDesk)
VALUES (@workshopAccountId, @customerId, @canRaiseInvoice, @isFrontDesk);
" + CheckEngineSql.SelectInsertedIntId() + ";",
            new DataParameter("workshopAccountId", technician.WorkshopAccountId),
            new DataParameter("customerId", technician.CustomerId),
            new DataParameter("canRaiseInvoice", technician.CanRaiseInvoice),
            new DataParameter("isFrontDesk", technician.IsFrontDesk));

        return id.FirstOrDefault();
    }

    public async Task<WorkshopTechnician?> GetTechnicianAsync(int workshopAccountId, int customerId, CancellationToken cancellationToken)
    {
        var sql = CheckEngineSql.SelectTop(1,
            "Id, WorkshopAccountId, CustomerId, CanRaiseInvoice, IsFrontDesk",
            "FROM TP_CE_WorkshopTechnician WHERE WorkshopAccountId = @workshopAccountId AND CustomerId = @customerId ORDER BY Id");
        var rows = await _dataProvider.QueryAsync<TechnicianRow>(sql,
            new DataParameter("workshopAccountId", workshopAccountId),
            new DataParameter("customerId", customerId));

        return rows.Select(row => new WorkshopTechnician
        {
            Id = row.Id,
            WorkshopAccountId = row.WorkshopAccountId,
            CustomerId = row.CustomerId,
            CanRaiseInvoice = row.CanRaiseInvoice,
            IsFrontDesk = row.IsFrontDesk
        }).FirstOrDefault();
    }

    public async Task<IReadOnlyList<WorkshopServiceHistoryEntry>> ListServiceHistoryForCustomerVehicleAsync(
        int workshopCustomerVehicleId,
        CancellationToken cancellationToken)
    {
        var rows = await _dataProvider.QueryAsync<ServiceHistoryRow>(@"
SELECT DISTINCT j.Id AS JobId, j.StatusId, j.LabourEstimate, j.OrderId, j.UpdatedUtc
FROM TP_CE_WorkshopJob j
INNER JOIN TP_CE_WorkshopJobVehicle jv ON jv.JobId = j.Id
INNER JOIN TP_CE_WorkshopCustomerVehicle cv ON cv.Id = @customerVehicleId
WHERE j.WorkshopCustomerId = cv.WorkshopCustomerId
  AND jv.VehicleConfigurationId = cv.VehicleConfigurationId
  AND (cv.Vin IS NULL OR jv.Vin IS NULL OR jv.Vin = cv.Vin)
ORDER BY j.UpdatedUtc DESC",
            new DataParameter("customerVehicleId", workshopCustomerVehicleId));

        return rows.Select(row => new WorkshopServiceHistoryEntry
        {
            JobId = row.JobId,
            Status = (WorkshopJobStatus)row.StatusId,
            LabourEstimate = row.LabourEstimate,
            OrderId = row.OrderId,
            UpdatedUtc = ToUtc(row.UpdatedUtc)
        }).ToList();
    }

    public Task UpdateJobLineAsync(WorkshopJobLine line, CancellationToken cancellationToken)
        => _dataProvider.ExecuteNonQueryAsync(@"
UPDATE TP_CE_WorkshopJobLine
SET InvoicedOrderId = @invoicedOrderId
WHERE Id = @id",
            new DataParameter("id", line.Id),
            new DataParameter("invoicedOrderId", line.InvoicedOrderId ?? (object)DBNull.Value));

    private static WorkshopAccount MapAccount(AccountRow row)
    {
        return new WorkshopAccount
        {
            Id = row.Id,
            CustomerId = row.CustomerId,
            DisplayName = row.DisplayName,
            CreditLimit = row.CreditLimit,
            CreditUsed = row.CreditUsed,
            DefaultPriceListId = row.DefaultPriceListId,
            IsActive = row.IsActive
        };
    }

    private static WorkshopJob MapJob(JobRow row)
    {
        return new WorkshopJob
        {
            Id = row.Id,
            WorkshopAccountId = row.WorkshopAccountId,
            WorkshopCustomerId = row.WorkshopCustomerId,
            AssignedTechnicianCustomerId = row.AssignedTechnicianCustomerId,
            Status = (WorkshopJobStatus)row.StatusId,
            LabourEstimate = row.LabourEstimate,
            OrderId = row.OrderId,
            CreatedUtc = ToUtc(row.CreatedUtc),
            UpdatedUtc = ToUtc(row.UpdatedUtc)
        };
    }

    private static WorkshopJobVehicle MapJobVehicle(JobVehicleRow row)
    {
        return new WorkshopJobVehicle
        {
            Id = row.Id,
            JobId = row.JobId,
            VehicleConfigurationId = row.VehicleConfigurationId,
            Vin = row.Vin,
            Label = row.Label
        };
    }

    private static WorkshopJobLine MapJobLine(JobLineRow row)
    {
        return new WorkshopJobLine
        {
            Id = row.Id,
            JobId = row.JobId,
            JobVehicleId = row.JobVehicleId,
            ProductId = row.ProductId,
            Quantity = row.Quantity,
            FitmentOutcome = row.FitmentOutcome,
            UnitPrice = row.UnitPrice,
            InvoicedOrderId = row.InvoicedOrderId
        };
    }

    private static DateTimeOffset ToUtc(DateTime value)
        => new(DateTime.SpecifyKind(value, DateTimeKind.Utc));

    private sealed class AccountRow
    {
        public int Id { get; set; }
        public int CustomerId { get; set; }
        public string DisplayName { get; set; } = string.Empty;
        public decimal CreditLimit { get; set; }
        public decimal CreditUsed { get; set; }
        public int? DefaultPriceListId { get; set; }
        public bool IsActive { get; set; }
    }

    private sealed class JobRow
    {
        public int Id { get; set; }
        public int WorkshopAccountId { get; set; }
        public int? WorkshopCustomerId { get; set; }
        public int? AssignedTechnicianCustomerId { get; set; }
        public int StatusId { get; set; }
        public decimal LabourEstimate { get; set; }
        public int? OrderId { get; set; }
        public DateTime CreatedUtc { get; set; }
        public DateTime UpdatedUtc { get; set; }
    }

    private sealed class JobVehicleRow
    {
        public int Id { get; set; }
        public int JobId { get; set; }
        public int VehicleConfigurationId { get; set; }
        public string? Vin { get; set; }
        public string? Label { get; set; }
    }

    private sealed class JobLineRow
    {
        public int Id { get; set; }
        public int JobId { get; set; }
        public int JobVehicleId { get; set; }
        public int ProductId { get; set; }
        public int Quantity { get; set; }
        public string FitmentOutcome { get; set; } = string.Empty;
        public decimal UnitPrice { get; set; }
        public int? InvoicedOrderId { get; set; }
    }

    private sealed class CustomerRow
    {
        public int Id { get; set; }
        public int WorkshopAccountId { get; set; }
        public string DisplayName { get; set; } = string.Empty;
        public string? ContactEmail { get; set; }
    }

    private sealed class CustomerVehicleRow
    {
        public int Id { get; set; }
        public int WorkshopCustomerId { get; set; }
        public int VehicleConfigurationId { get; set; }
        public string? Vin { get; set; }
    }

    private sealed class TechnicianRow
    {
        public int Id { get; set; }
        public int WorkshopAccountId { get; set; }
        public int CustomerId { get; set; }
        public bool CanRaiseInvoice { get; set; }
        public bool IsFrontDesk { get; set; }
    }

    private sealed class ServiceHistoryRow
    {
        public int JobId { get; set; }
        public int StatusId { get; set; }
        public decimal LabourEstimate { get; set; }
        public int? OrderId { get; set; }
        public DateTime UpdatedUtc { get; set; }
    }
}
