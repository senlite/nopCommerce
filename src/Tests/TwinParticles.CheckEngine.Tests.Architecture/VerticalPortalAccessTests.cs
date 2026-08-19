using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using NUnit.Framework;
using TwinParticles.CheckEngine.Application.Portals;
using TwinParticles.CheckEngine.Domain.Dealer;
using TwinParticles.CheckEngine.Domain.Fleet;
using TwinParticles.CheckEngine.Domain.Workshop;

namespace TwinParticles.CheckEngine.Tests.Architecture;

[TestFixture]
public class VerticalPortalAccessTests
{
    [Test]
    public async Task Workshop_Access_Should_Allow_Provisioned_Customer()
    {
        var service = new VerticalPortalAccessService(
            new StubWorkshopRepo(customerId: 42, accountId: 7),
            new StubFleetRepo(),
            new StubDealerRepo());

        var result = await service.ResolveWorkshopAsync(42, isOperator: false, CancellationToken.None);

        result.Allowed.Should().BeTrue();
        result.AccountId.Should().Be(7);
    }

    [Test]
    public async Task Workshop_Access_Should_Deny_Unprovisioned_Customer()
    {
        var service = new VerticalPortalAccessService(
            new StubWorkshopRepo(customerId: null, accountId: null),
            new StubFleetRepo(),
            new StubDealerRepo());

        var result = await service.ResolveWorkshopAsync(99, isOperator: false, CancellationToken.None);

        result.Allowed.Should().BeFalse();
        result.ErrorCode.Should().Be(PortalErrorCodes.AccountNotProvisioned);
    }

    [Test]
    public async Task Workshop_Access_Should_Allow_Operator_Without_Account()
    {
        var service = new VerticalPortalAccessService(
            new StubWorkshopRepo(customerId: null, accountId: null),
            new StubFleetRepo(),
            new StubDealerRepo());

        var result = await service.ResolveWorkshopAsync(99, isOperator: true, CancellationToken.None);

        result.Allowed.Should().BeTrue();
        result.IsOperator.Should().BeTrue();
    }

    private sealed class StubWorkshopRepo : IWorkshopJobRepository
    {
        private readonly int? _customerId;
        private readonly int? _accountId;

        public StubWorkshopRepo(int? customerId, int? accountId)
        {
            _customerId = customerId;
            _accountId = accountId;
        }

        public Task<WorkshopAccount?> GetAccountByCustomerIdAsync(int customerId, CancellationToken cancellationToken)
        {
            if (_customerId != customerId)
                return Task.FromResult<WorkshopAccount?>(null);

            return Task.FromResult<WorkshopAccount?>(new WorkshopAccount
            {
                Id = _accountId ?? 0,
                CustomerId = customerId,
                DisplayName = "Test",
                IsActive = true
            });
        }

        public Task<WorkshopAccount?> ResolveAccountForPortalUserAsync(int customerId, CancellationToken cancellationToken)
            => GetAccountByCustomerIdAsync(customerId, cancellationToken);

        public Task<WorkshopAccount?> GetAccountByIdAsync(int accountId, CancellationToken cancellationToken)
        {
            if (_accountId != accountId)
                return Task.FromResult<WorkshopAccount?>(null);

            return Task.FromResult<WorkshopAccount?>(new WorkshopAccount
            {
                Id = accountId,
                CustomerId = _customerId ?? 0,
                DisplayName = "Test",
                IsActive = true
            });
        }

        public Task<int> InsertAccountAsync(WorkshopAccount account, CancellationToken cancellationToken) => throw new System.NotImplementedException();
        public Task UpdateAccountAsync(WorkshopAccount account, CancellationToken cancellationToken) => throw new System.NotImplementedException();
        public Task<int> InsertPriceListAsync(string name, CancellationToken cancellationToken) => throw new System.NotImplementedException();
        public Task InsertPriceListItemAsync(int priceListId, int productId, decimal unitPrice, CancellationToken cancellationToken) => throw new System.NotImplementedException();
        public Task<WorkshopJob?> GetJobAsync(int jobId, CancellationToken cancellationToken) => throw new System.NotImplementedException();
        public Task<int> InsertJobAsync(WorkshopJob job, CancellationToken cancellationToken) => throw new System.NotImplementedException();
        public Task UpdateJobAsync(WorkshopJob job, CancellationToken cancellationToken) => throw new System.NotImplementedException();
        public Task<int> InsertJobVehicleAsync(WorkshopJobVehicle vehicle, CancellationToken cancellationToken) => throw new System.NotImplementedException();
        public Task<IReadOnlyList<WorkshopJobVehicle>> GetJobVehiclesAsync(int jobId, CancellationToken cancellationToken) => throw new System.NotImplementedException();
        public Task<int> InsertJobLineAsync(WorkshopJobLine line, CancellationToken cancellationToken) => throw new System.NotImplementedException();
        public Task<IReadOnlyList<WorkshopJobLine>> GetJobLinesAsync(int jobId, CancellationToken cancellationToken) => throw new System.NotImplementedException();
        public Task<WorkshopJobVehicle?> GetJobVehicleAsync(int jobVehicleId, CancellationToken cancellationToken) => throw new System.NotImplementedException();
        public Task<IReadOnlyList<WorkshopJob>> ListJobsByAccountAsync(int workshopAccountId, CancellationToken cancellationToken) => throw new System.NotImplementedException();
        public Task<IReadOnlyList<WorkshopJob>> ListJobsByAccountAsync(int workshopAccountId, int? assignedTechnicianCustomerId, CancellationToken cancellationToken) => throw new System.NotImplementedException();
        public Task<decimal> ResolveTradePriceAsync(int priceListId, int productId, CancellationToken cancellationToken) => throw new System.NotImplementedException();
        public Task<int> InsertCustomerAsync(WorkshopCustomer customer, CancellationToken cancellationToken) => throw new System.NotImplementedException();
        public Task<int> InsertCustomerVehicleAsync(WorkshopCustomerVehicle vehicle, CancellationToken cancellationToken) => throw new System.NotImplementedException();
        public Task<IReadOnlyList<WorkshopCustomer>> ListCustomersAsync(int workshopAccountId, CancellationToken cancellationToken) => throw new System.NotImplementedException();
        public Task<IReadOnlyList<WorkshopCustomerVehicle>> ListCustomerVehiclesAsync(int workshopCustomerId, CancellationToken cancellationToken) => throw new System.NotImplementedException();
        public Task<int> InsertTechnicianAsync(WorkshopTechnician technician, CancellationToken cancellationToken) => throw new System.NotImplementedException();
        public Task<WorkshopTechnician?> GetTechnicianAsync(int workshopAccountId, int customerId, CancellationToken cancellationToken) => throw new System.NotImplementedException();
        public Task UpdateJobLineAsync(WorkshopJobLine line, CancellationToken cancellationToken) => throw new System.NotImplementedException();
        public Task<IReadOnlyList<WorkshopServiceHistoryEntry>> ListServiceHistoryForCustomerVehicleAsync(int workshopCustomerVehicleId, CancellationToken cancellationToken) => throw new System.NotImplementedException();
        public Task<WorkshopCustomer?> GetCustomerAsync(int workshopCustomerId, CancellationToken cancellationToken) => throw new System.NotImplementedException();
        public Task<int> InsertLabourRateAsync(WorkshopLabourRate rate, CancellationToken cancellationToken) => throw new System.NotImplementedException();
        public Task<WorkshopLabourRate?> GetLabourRateAsync(int workshopAccountId, string operationCode, CancellationToken cancellationToken) => throw new System.NotImplementedException();
        public Task<IReadOnlyList<WorkshopInvoicedJobSummary>> ListInvoicedJobsInPeriodAsync(int workshopAccountId, System.DateTime periodStartUtc, System.DateTime periodEndUtc, CancellationToken cancellationToken) => throw new System.NotImplementedException();
        public Task<int> InsertCreditStatementAsync(WorkshopCreditStatement statement, CancellationToken cancellationToken) => throw new System.NotImplementedException();
        public Task InsertCreditStatementLineAsync(WorkshopCreditStatementLine line, CancellationToken cancellationToken) => throw new System.NotImplementedException();
        public Task<IReadOnlyList<WorkshopCreditStatement>> ListCreditStatementsAsync(int workshopAccountId, CancellationToken cancellationToken) => throw new System.NotImplementedException();
        public Task<WorkshopCreditStatement?> GetCreditStatementAsync(int statementId, CancellationToken cancellationToken) => throw new System.NotImplementedException();
    }

    private sealed class StubFleetRepo : IFleetPortalRepository
    {
        public Task<FleetAccount?> GetAccountByCustomerIdAsync(int customerId, CancellationToken cancellationToken) => Task.FromResult<FleetAccount?>(null);
        public Task<FleetAccount?> GetAccountByIdAsync(int fleetAccountId, CancellationToken cancellationToken) => Task.FromResult<FleetAccount?>(null);
        public Task<int> InsertAccountAsync(FleetAccount account, CancellationToken cancellationToken) => throw new System.NotImplementedException();
        public Task<int> InsertBudgetCentreAsync(FleetBudgetCentre centre, CancellationToken cancellationToken) => throw new System.NotImplementedException();
        public Task<IReadOnlyList<FleetBudgetCentre>> ListBudgetCentresByAccountAsync(int fleetAccountId, CancellationToken cancellationToken) => throw new System.NotImplementedException();
        public Task UpdateBudgetCentreAsync(FleetBudgetCentre centre, CancellationToken cancellationToken) => throw new System.NotImplementedException();
        public Task<int> InsertVehicleAsync(FleetVehicle vehicle, CancellationToken cancellationToken) => throw new System.NotImplementedException();
        public Task<FleetVehicle?> GetVehicleAsync(int vehicleId, CancellationToken cancellationToken) => throw new System.NotImplementedException();
        public Task<IReadOnlyList<FleetVehicle>> GetVehiclesAsync(int fleetAccountId, CancellationToken cancellationToken) => throw new System.NotImplementedException();
        public Task<int> InsertImportBatchAsync(FleetVinImportBatch batch, CancellationToken cancellationToken) => throw new System.NotImplementedException();
        public Task<FleetBudgetCentre?> GetBudgetCentreAsync(int budgetCentreId, CancellationToken cancellationToken) => throw new System.NotImplementedException();
        public Task<int> InsertApprovalRequestAsync(FleetApprovalRequest request, CancellationToken cancellationToken) => throw new System.NotImplementedException();
        public Task UpdateApprovalRequestAsync(FleetApprovalRequest request, CancellationToken cancellationToken) => throw new System.NotImplementedException();
        public Task<FleetApprovalRequest?> GetApprovalRequestAsync(int requestId, CancellationToken cancellationToken) => throw new System.NotImplementedException();
        public Task<IReadOnlyList<FleetApprovalRequest>> ListApprovalRequestsByAccountAsync(int fleetAccountId, CancellationToken cancellationToken) => throw new System.NotImplementedException();
        public Task<IReadOnlyList<FleetMaintenanceSchedule>> ListMaintenanceSchedulesAsync(int fleetAccountId, CancellationToken cancellationToken) => throw new System.NotImplementedException();
        public Task<int> InsertMaintenanceScheduleAsync(FleetMaintenanceSchedule schedule, CancellationToken cancellationToken) => throw new System.NotImplementedException();
        public Task<IReadOnlyList<FleetMaintenanceForecast>> ListMaintenanceForecastsAsync(int fleetAccountId, CancellationToken cancellationToken) => throw new System.NotImplementedException();
        public Task UpsertMaintenanceForecastAsync(FleetMaintenanceForecast forecast, CancellationToken cancellationToken) => throw new System.NotImplementedException();
        public Task<int> InsertVehicleSpendAsync(FleetVehicleSpend spend, CancellationToken cancellationToken) => throw new System.NotImplementedException();
        public Task<IReadOnlyList<FleetVehicleCostSummary>> ListVehicleCostSummariesAsync(int fleetAccountId, CancellationToken cancellationToken) => throw new System.NotImplementedException();
        public Task<IReadOnlyList<FleetVinImportBatch>> ListImportBatchesAsync(int fleetAccountId, CancellationToken cancellationToken) => throw new System.NotImplementedException();
        public Task<FleetVinImportBatchDetail?> GetImportBatchDetailAsync(int batchId, CancellationToken cancellationToken) => throw new System.NotImplementedException();
        public Task<int> InsertMemberAsync(FleetMember member, CancellationToken cancellationToken) => throw new System.NotImplementedException();
        public Task<FleetMember?> GetMemberAsync(int fleetAccountId, int customerId, CancellationToken cancellationToken) => throw new System.NotImplementedException();
    }

    private sealed class StubDealerRepo : IDealerPortalRepository
    {
        public Task<DealerAccount?> GetAccountByCustomerIdAsync(int customerId, CancellationToken cancellationToken) => Task.FromResult<DealerAccount?>(null);
        public Task<DealerAccount?> GetAccountByIdAsync(int dealerAccountId, CancellationToken cancellationToken) => Task.FromResult<DealerAccount?>(null);
        public Task<int> InsertAccountAsync(DealerAccount account, CancellationToken cancellationToken) => throw new System.NotImplementedException();
        public Task<IReadOnlyList<DealerFranchise>> GetFranchisesAsync(int dealerAccountId, CancellationToken cancellationToken) => throw new System.NotImplementedException();
        public Task<IReadOnlyList<DealerCatalogItem>> GetCatalogViewAsync(int dealerAccountId, CancellationToken cancellationToken) => throw new System.NotImplementedException();
        public Task<DealerQuota?> GetQuotaAsync(int dealerAccountId, CancellationToken cancellationToken) => throw new System.NotImplementedException();
        public Task UpdateQuotaAsync(DealerQuota quota, CancellationToken cancellationToken) => throw new System.NotImplementedException();
        public Task<DealerAllocation?> GetAllocationForProductAsync(int dealerAccountId, int productId, CancellationToken cancellationToken) => throw new System.NotImplementedException();
        public Task UpdateAllocationAsync(DealerAllocation allocation, CancellationToken cancellationToken) => throw new System.NotImplementedException();
        public Task<int> InsertWarrantyClaimAsync(WarrantyClaim claim, CancellationToken cancellationToken) => throw new System.NotImplementedException();
        public Task UpdateWarrantyClaimAsync(WarrantyClaim claim, CancellationToken cancellationToken) => throw new System.NotImplementedException();
        public Task<WarrantyClaim?> GetWarrantyClaimAsync(int claimId, CancellationToken cancellationToken) => throw new System.NotImplementedException();
        public Task<IReadOnlyList<WarrantyClaim>> ListWarrantyClaimsByAccountAsync(int dealerAccountId, CancellationToken cancellationToken) => throw new System.NotImplementedException();
        public Task<int> InsertAllocationAsync(DealerAllocation allocation, CancellationToken cancellationToken) => throw new System.NotImplementedException();
        public Task<int> InsertQuotaAsync(DealerQuota quota, CancellationToken cancellationToken) => throw new System.NotImplementedException();
        public Task<int> InsertFranchiseAsync(DealerFranchise franchise, CancellationToken cancellationToken) => throw new System.NotImplementedException();
        public Task UpdateAccountAsync(DealerAccount account, CancellationToken cancellationToken) => throw new System.NotImplementedException();
        public Task<int> InsertPriceListAsync(string name, CancellationToken cancellationToken) => throw new System.NotImplementedException();
        public Task InsertPriceListItemAsync(int priceListId, int productId, decimal unitPrice, CancellationToken cancellationToken) => throw new System.NotImplementedException();
        public Task<IReadOnlyList<DealerTerritory>> GetTerritoriesAsync(int dealerAccountId, CancellationToken cancellationToken) => throw new System.NotImplementedException();
        public Task<int> InsertTerritoryAsync(DealerTerritory territory, CancellationToken cancellationToken) => throw new System.NotImplementedException();
        public Task<bool> IsVehicleMarketAllowedAsync(int vehicleConfigurationId, IReadOnlyList<DealerTerritory> territories, CancellationToken cancellationToken) => throw new System.NotImplementedException();
    }
}
