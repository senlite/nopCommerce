using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace TwinParticles.CheckEngine.Domain.Workshop;

public interface IWorkshopJobRepository
{
    Task<WorkshopAccount?> GetAccountByCustomerIdAsync(int customerId, CancellationToken cancellationToken);

    Task<WorkshopAccount?> ResolveAccountForPortalUserAsync(int customerId, CancellationToken cancellationToken);

    Task<WorkshopAccount?> GetAccountByIdAsync(int accountId, CancellationToken cancellationToken);

    Task<int> InsertJobAsync(WorkshopJob job, CancellationToken cancellationToken);

    Task UpdateJobAsync(WorkshopJob job, CancellationToken cancellationToken);

    Task<WorkshopJob?> GetJobAsync(int jobId, CancellationToken cancellationToken);

    Task<int> InsertJobVehicleAsync(WorkshopJobVehicle vehicle, CancellationToken cancellationToken);

    Task<IReadOnlyList<WorkshopJobVehicle>> GetJobVehiclesAsync(int jobId, CancellationToken cancellationToken);

    Task<WorkshopJobVehicle?> GetJobVehicleAsync(int jobVehicleId, CancellationToken cancellationToken);

    Task<int> InsertJobLineAsync(WorkshopJobLine line, CancellationToken cancellationToken);

    Task<IReadOnlyList<WorkshopJobLine>> GetJobLinesAsync(int jobId, CancellationToken cancellationToken);

    Task<IReadOnlyList<WorkshopJob>> ListJobsByAccountAsync(int workshopAccountId, CancellationToken cancellationToken);

    Task<IReadOnlyList<WorkshopJob>> ListJobsByAccountAsync(
        int workshopAccountId,
        int? assignedTechnicianCustomerId,
        CancellationToken cancellationToken);

    Task<int> InsertAccountAsync(WorkshopAccount account, CancellationToken cancellationToken);

    Task UpdateAccountAsync(WorkshopAccount account, CancellationToken cancellationToken);

    Task<int> InsertPriceListAsync(string name, CancellationToken cancellationToken);

    Task InsertPriceListItemAsync(int priceListId, int productId, decimal unitPrice, CancellationToken cancellationToken);

    Task<decimal> ResolveTradePriceAsync(int priceListId, int productId, CancellationToken cancellationToken);

    Task<int> InsertCustomerAsync(WorkshopCustomer customer, CancellationToken cancellationToken);

    Task<int> InsertCustomerVehicleAsync(WorkshopCustomerVehicle vehicle, CancellationToken cancellationToken);

    Task<IReadOnlyList<WorkshopCustomer>> ListCustomersAsync(int workshopAccountId, CancellationToken cancellationToken);

    Task<IReadOnlyList<WorkshopCustomerVehicle>> ListCustomerVehiclesAsync(int workshopCustomerId, CancellationToken cancellationToken);

    Task<int> InsertTechnicianAsync(WorkshopTechnician technician, CancellationToken cancellationToken);

    Task<WorkshopTechnician?> GetTechnicianAsync(int workshopAccountId, int customerId, CancellationToken cancellationToken);

    Task UpdateJobLineAsync(WorkshopJobLine line, CancellationToken cancellationToken);

    Task<IReadOnlyList<WorkshopServiceHistoryEntry>> ListServiceHistoryForCustomerVehicleAsync(
        int workshopCustomerVehicleId,
        CancellationToken cancellationToken);

    Task<WorkshopCustomer?> GetCustomerAsync(int workshopCustomerId, CancellationToken cancellationToken);

    Task<int> InsertLabourRateAsync(WorkshopLabourRate rate, CancellationToken cancellationToken);

    Task<WorkshopLabourRate?> GetLabourRateAsync(int workshopAccountId, string operationCode, CancellationToken cancellationToken);

    Task<IReadOnlyList<WorkshopInvoicedJobSummary>> ListInvoicedJobsInPeriodAsync(
        int workshopAccountId,
        DateTime periodStartUtc,
        DateTime periodEndUtc,
        CancellationToken cancellationToken);

    Task<int> InsertCreditStatementAsync(WorkshopCreditStatement statement, CancellationToken cancellationToken);

    Task InsertCreditStatementLineAsync(WorkshopCreditStatementLine line, CancellationToken cancellationToken);

    Task<IReadOnlyList<WorkshopCreditStatement>> ListCreditStatementsAsync(int workshopAccountId, CancellationToken cancellationToken);

    Task<WorkshopCreditStatement?> GetCreditStatementAsync(int statementId, CancellationToken cancellationToken);
}
