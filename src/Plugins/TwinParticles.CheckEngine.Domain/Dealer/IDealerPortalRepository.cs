using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace TwinParticles.CheckEngine.Domain.Dealer;

public interface IDealerPortalRepository
{
    Task<DealerAccount?> GetAccountByCustomerIdAsync(int customerId, CancellationToken cancellationToken);

    Task<DealerAccount?> GetAccountByIdAsync(int accountId, CancellationToken cancellationToken);

    Task<IReadOnlyList<DealerFranchise>> GetFranchisesAsync(int dealerAccountId, CancellationToken cancellationToken);

    Task<IReadOnlyList<DealerCatalogItem>> GetCatalogViewAsync(int dealerAccountId, CancellationToken cancellationToken);

    Task<DealerAllocation?> GetAllocationForProductAsync(int dealerAccountId, int productId, CancellationToken cancellationToken);

    Task<DealerQuota?> GetQuotaAsync(int dealerAccountId, CancellationToken cancellationToken);

    Task UpdateAllocationAsync(DealerAllocation allocation, CancellationToken cancellationToken);

    Task UpdateQuotaAsync(DealerQuota quota, CancellationToken cancellationToken);

    Task<int> InsertWarrantyClaimAsync(WarrantyClaim claim, CancellationToken cancellationToken);

    Task UpdateWarrantyClaimAsync(WarrantyClaim claim, CancellationToken cancellationToken);

    Task<WarrantyClaim?> GetWarrantyClaimAsync(int claimId, CancellationToken cancellationToken);

    Task<IReadOnlyList<WarrantyClaim>> ListWarrantyClaimsByAccountAsync(int dealerAccountId, CancellationToken cancellationToken);

    Task<int> InsertAccountAsync(DealerAccount account, CancellationToken cancellationToken);

    Task<int> InsertAllocationAsync(DealerAllocation allocation, CancellationToken cancellationToken);

    Task<int> InsertQuotaAsync(DealerQuota quota, CancellationToken cancellationToken);
}
