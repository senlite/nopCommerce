using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace TwinParticles.CheckEngine.Domain.Marketplace;

public interface ICommissionPlanRepository
{
    Task<CommissionPlan?> GetActivePlanAsync(int vendorId, CancellationToken cancellationToken);

    Task UpsertPlanAsync(CommissionPlan plan, CancellationToken cancellationToken);
}

public interface ICommissionSnapshotStore
{
    Task SaveSnapshotsAsync(IReadOnlyCollection<OrderLineCommissionSnapshot> snapshots, CancellationToken cancellationToken);

    Task<IReadOnlyList<OrderLineCommissionSnapshot>> GetByOrderIdAsync(int orderId, CancellationToken cancellationToken);

    Task<decimal> GetVendorMonthVolumeExclTaxAsync(
        int vendorId,
        int year,
        int month,
        int excludeOrderId,
        CancellationToken cancellationToken);
}

public interface IVendorProductCategoryStore
{
    Task<IReadOnlyDictionary<int, IReadOnlyList<int>>> GetCategoryIdsByProductIdsAsync(
        IReadOnlyCollection<int> productIds,
        CancellationToken cancellationToken);
}
