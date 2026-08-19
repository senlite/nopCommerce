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

    public async Task<WorkshopAccount?> ResolveAccountForPortalUserAsync(int customerId, CancellationToken cancellationToken)
    {
        var owned = await GetAccountByCustomerIdAsync(customerId, cancellationToken);
        if (owned is not null)
            return owned;

        var technicianRows = await _dataProvider.QueryAsync<TechnicianAccountRow>(CheckEngineSql.SelectTop(1,
            "t.WorkshopAccountId",
            @"FROM TP_CE_WorkshopTechnician t
INNER JOIN TP_CE_WorkshopAccount a ON a.Id = t.WorkshopAccountId
WHERE t.CustomerId = @customerId AND a.IsActive = 1
ORDER BY t.Id"),
            new DataParameter("customerId", customerId));

        var accountId = technicianRows.Select(row => row.WorkshopAccountId).FirstOrDefault();
        return accountId > 0 ? await GetAccountByIdAsync(accountId, cancellationToken) : null;
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

    public Task<IReadOnlyList<WorkshopJob>> ListJobsByAccountAsync(int workshopAccountId, CancellationToken cancellationToken)
        => ListJobsByAccountAsync(workshopAccountId, assignedTechnicianCustomerId: null, cancellationToken);

    public async Task<IReadOnlyList<WorkshopJob>> ListJobsByAccountAsync(
        int workshopAccountId,
        int? assignedTechnicianCustomerId,
        CancellationToken cancellationToken)
    {
        var filter = assignedTechnicianCustomerId.HasValue
            ? " AND AssignedTechnicianCustomerId = @technicianCustomerId"
            : string.Empty;

        var rows = await _dataProvider.QueryAsync<JobRow>($@"
SELECT Id, WorkshopAccountId, WorkshopCustomerId, AssignedTechnicianCustomerId, StatusId, LabourEstimate, OrderId, CreatedUtc, UpdatedUtc
FROM TP_CE_WorkshopJob
WHERE WorkshopAccountId = @accountId{filter}
ORDER BY UpdatedUtc DESC",
            new DataParameter("accountId", workshopAccountId),
            new DataParameter("technicianCustomerId", assignedTechnicianCustomerId ?? (object)DBNull.Value));

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

    public async Task<WorkshopCustomer?> GetCustomerAsync(int workshopCustomerId, CancellationToken cancellationToken)
    {
        var rows = await _dataProvider.QueryAsync<CustomerRow>(@"
SELECT Id, WorkshopAccountId, DisplayName, ContactEmail
FROM TP_CE_WorkshopCustomer
WHERE Id = @id",
            new DataParameter("id", workshopCustomerId));

        return rows.Select(row => new WorkshopCustomer
        {
            Id = row.Id,
            WorkshopAccountId = row.WorkshopAccountId,
            DisplayName = row.DisplayName,
            ContactEmail = row.ContactEmail
        }).FirstOrDefault();
    }

    public async Task<int> InsertLabourRateAsync(WorkshopLabourRate rate, CancellationToken cancellationToken)
    {
        var id = await _dataProvider.QueryAsync<int>(@"
INSERT INTO TP_CE_WorkshopLabourRate (WorkshopAccountId, OperationCode, HourlyRate)
VALUES (@workshopAccountId, @operationCode, @hourlyRate);
" + CheckEngineSql.SelectInsertedIntId() + ";",
            new DataParameter("workshopAccountId", rate.WorkshopAccountId),
            new DataParameter("operationCode", rate.OperationCode),
            new DataParameter("hourlyRate", rate.HourlyRate));

        return id.FirstOrDefault();
    }

    public async Task<WorkshopLabourRate?> GetLabourRateAsync(int workshopAccountId, string operationCode, CancellationToken cancellationToken)
    {
        var sql = CheckEngineSql.SelectTop(1,
            "Id, WorkshopAccountId, OperationCode, HourlyRate",
            "FROM TP_CE_WorkshopLabourRate WHERE WorkshopAccountId = @workshopAccountId AND OperationCode = @operationCode ORDER BY Id");
        var rows = await _dataProvider.QueryAsync<LabourRateRow>(sql,
            new DataParameter("workshopAccountId", workshopAccountId),
            new DataParameter("operationCode", operationCode));

        return rows.Select(row => new WorkshopLabourRate
        {
            Id = row.Id,
            WorkshopAccountId = row.WorkshopAccountId,
            OperationCode = row.OperationCode,
            HourlyRate = row.HourlyRate
        }).FirstOrDefault();
    }

    public async Task<IReadOnlyList<WorkshopInvoicedJobSummary>> ListInvoicedJobsInPeriodAsync(
        int workshopAccountId,
        DateTime periodStartUtc,
        DateTime periodEndUtc,
        CancellationToken cancellationToken)
    {
        var rows = await _dataProvider.QueryAsync<InvoicedJobRow>(@"
SELECT j.Id AS JobId,
       j.OrderId,
       j.LabourEstimate,
       j.UpdatedUtc AS InvoicedUtc,
       COALESCE(SUM(l.UnitPrice * l.Quantity), 0) AS PartsTotal
FROM TP_CE_WorkshopJob j
LEFT JOIN TP_CE_WorkshopJobLine l ON l.JobId = j.Id AND l.InvoicedOrderId IS NOT NULL
WHERE j.WorkshopAccountId = @accountId
  AND j.StatusId = @invoicedStatus
  AND j.UpdatedUtc >= @periodStartUtc
  AND j.UpdatedUtc < @periodEndUtc
GROUP BY j.Id, j.OrderId, j.LabourEstimate, j.UpdatedUtc
ORDER BY j.UpdatedUtc",
            new DataParameter("accountId", workshopAccountId),
            new DataParameter("invoicedStatus", (int)WorkshopJobStatus.Invoiced),
            new DataParameter("periodStartUtc", periodStartUtc),
            new DataParameter("periodEndUtc", periodEndUtc));

        return rows.Select(row => new WorkshopInvoicedJobSummary
        {
            JobId = row.JobId,
            OrderId = row.OrderId,
            LabourEstimate = row.LabourEstimate,
            PartsTotal = row.PartsTotal,
            InvoicedUtc = ToUtc(row.InvoicedUtc).UtcDateTime
        }).ToList();
    }

    public async Task<int> InsertCreditStatementAsync(WorkshopCreditStatement statement, CancellationToken cancellationToken)
    {
        var id = await _dataProvider.QueryAsync<int>(@"
INSERT INTO TP_CE_WorkshopCreditStatement
(WorkshopAccountId, PeriodStartUtc, PeriodEndUtc, OpeningBalance, InvoicedTotal, ClosingBalance, CreatedUtc)
VALUES
(@workshopAccountId, @periodStartUtc, @periodEndUtc, @openingBalance, @invoicedTotal, @closingBalance, @createdUtc);
" + CheckEngineSql.SelectInsertedIntId() + ";",
            new DataParameter("workshopAccountId", statement.WorkshopAccountId),
            new DataParameter("periodStartUtc", statement.PeriodStartUtc),
            new DataParameter("periodEndUtc", statement.PeriodEndUtc),
            new DataParameter("openingBalance", statement.OpeningBalance),
            new DataParameter("invoicedTotal", statement.InvoicedTotal),
            new DataParameter("closingBalance", statement.ClosingBalance),
            new DataParameter("createdUtc", statement.CreatedUtc));

        return id.FirstOrDefault();
    }

    public Task InsertCreditStatementLineAsync(WorkshopCreditStatementLine line, CancellationToken cancellationToken)
        => _dataProvider.ExecuteNonQueryAsync(@"
INSERT INTO TP_CE_WorkshopCreditStatementLine
(StatementId, JobId, OrderId, PartsTotal, LabourTotal, LineTotal, InvoicedUtc)
VALUES
(@statementId, @jobId, @orderId, @partsTotal, @labourTotal, @lineTotal, @invoicedUtc)",
            new DataParameter("statementId", line.StatementId),
            new DataParameter("jobId", line.JobId),
            new DataParameter("orderId", line.OrderId ?? (object)DBNull.Value),
            new DataParameter("partsTotal", line.PartsTotal),
            new DataParameter("labourTotal", line.LabourTotal),
            new DataParameter("lineTotal", line.LineTotal),
            new DataParameter("invoicedUtc", line.InvoicedUtc));

    public async Task<IReadOnlyList<WorkshopCreditStatement>> ListCreditStatementsAsync(int workshopAccountId, CancellationToken cancellationToken)
    {
        var rows = await _dataProvider.QueryAsync<CreditStatementRow>(@"
SELECT Id, WorkshopAccountId, PeriodStartUtc, PeriodEndUtc, OpeningBalance, InvoicedTotal, ClosingBalance, CreatedUtc
FROM TP_CE_WorkshopCreditStatement
WHERE WorkshopAccountId = @accountId
ORDER BY PeriodEndUtc DESC",
            new DataParameter("accountId", workshopAccountId));

        return rows.Select(row => new WorkshopCreditStatement
        {
            Id = row.Id,
            WorkshopAccountId = row.WorkshopAccountId,
            PeriodStartUtc = row.PeriodStartUtc,
            PeriodEndUtc = row.PeriodEndUtc,
            OpeningBalance = row.OpeningBalance,
            InvoicedTotal = row.InvoicedTotal,
            ClosingBalance = row.ClosingBalance,
            CreatedUtc = row.CreatedUtc
        }).ToList();
    }

    public async Task<WorkshopCreditStatement?> GetCreditStatementAsync(int statementId, CancellationToken cancellationToken)
    {
        var rows = await _dataProvider.QueryAsync<CreditStatementRow>(@"
SELECT Id, WorkshopAccountId, PeriodStartUtc, PeriodEndUtc, OpeningBalance, InvoicedTotal, ClosingBalance, CreatedUtc
FROM TP_CE_WorkshopCreditStatement
WHERE Id = @id",
            new DataParameter("id", statementId));

        var statement = rows.Select(row => new WorkshopCreditStatement
        {
            Id = row.Id,
            WorkshopAccountId = row.WorkshopAccountId,
            PeriodStartUtc = row.PeriodStartUtc,
            PeriodEndUtc = row.PeriodEndUtc,
            OpeningBalance = row.OpeningBalance,
            InvoicedTotal = row.InvoicedTotal,
            ClosingBalance = row.ClosingBalance,
            CreatedUtc = row.CreatedUtc
        }).FirstOrDefault();

        if (statement is null)
            return null;

        var lineRows = await _dataProvider.QueryAsync<CreditStatementLineRow>(@"
SELECT Id, StatementId, JobId, OrderId, PartsTotal, LabourTotal, LineTotal, InvoicedUtc
FROM TP_CE_WorkshopCreditStatementLine
WHERE StatementId = @statementId
ORDER BY InvoicedUtc",
            new DataParameter("statementId", statementId));

        statement.Lines = lineRows.Select(row => new WorkshopCreditStatementLine
        {
            Id = row.Id,
            StatementId = row.StatementId,
            JobId = row.JobId,
            OrderId = row.OrderId,
            PartsTotal = row.PartsTotal,
            LabourTotal = row.LabourTotal,
            LineTotal = row.LineTotal,
            InvoicedUtc = row.InvoicedUtc
        }).ToList();

        return statement;
    }

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

    private sealed class TechnicianAccountRow
    {
        public int WorkshopAccountId { get; set; }
    }

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

    private sealed class LabourRateRow
    {
        public int Id { get; set; }
        public int WorkshopAccountId { get; set; }
        public string OperationCode { get; set; } = string.Empty;
        public decimal HourlyRate { get; set; }
    }

    private sealed class InvoicedJobRow
    {
        public int JobId { get; set; }
        public int? OrderId { get; set; }
        public decimal LabourEstimate { get; set; }
        public decimal PartsTotal { get; set; }
        public DateTime InvoicedUtc { get; set; }
    }

    private sealed class CreditStatementRow
    {
        public int Id { get; set; }
        public int WorkshopAccountId { get; set; }
        public DateTime PeriodStartUtc { get; set; }
        public DateTime PeriodEndUtc { get; set; }
        public decimal OpeningBalance { get; set; }
        public decimal InvoicedTotal { get; set; }
        public decimal ClosingBalance { get; set; }
        public DateTime CreatedUtc { get; set; }
    }

    private sealed class CreditStatementLineRow
    {
        public int Id { get; set; }
        public int StatementId { get; set; }
        public int JobId { get; set; }
        public int? OrderId { get; set; }
        public decimal PartsTotal { get; set; }
        public decimal LabourTotal { get; set; }
        public decimal LineTotal { get; set; }
        public DateTime InvoicedUtc { get; set; }
    }
}
