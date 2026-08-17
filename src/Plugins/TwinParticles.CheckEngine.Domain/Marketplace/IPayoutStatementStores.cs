using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace TwinParticles.CheckEngine.Domain.Marketplace;

public interface IPayoutStatementRepository
{
    Task<int> InsertAsync(PayoutStatement statement, CancellationToken cancellationToken);

    Task UpdateAsync(PayoutStatement statement, CancellationToken cancellationToken);

    Task<PayoutStatement?> GetByIdAsync(int statementId, CancellationToken cancellationToken);

    Task<IReadOnlyList<PayoutStatement>> ListByVendorAsync(int vendorId, CancellationToken cancellationToken);

    Task<PayoutStatement?> GetLatestByVendorAsync(int vendorId, CancellationToken cancellationToken);

    Task InsertAdjustmentAsync(PayoutAdjustment adjustment, CancellationToken cancellationToken);

    Task<IReadOnlyList<PayoutAdjustment>> GetPendingAdjustmentsAsync(int vendorId, CancellationToken cancellationToken);

    Task AssignAdjustmentsToStatementAsync(int vendorId, int statementId, CancellationToken cancellationToken);
}

public interface IPayoutStatementDataSource
{
    Task<IReadOnlyList<PayoutSourceLine>> GetVendorLinesAsync(
        int vendorId,
        DateTime periodStartUtc,
        DateTime periodEndUtc,
        CancellationToken cancellationToken);
}
