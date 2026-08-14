using System;
using System.Threading;
using System.Threading.Tasks;

namespace TwinParticles.CheckEngine.Domain.Erp;

/// <summary>
/// Supplies the local and ERP totals compared during daily reconciliation. Implementations read
/// the nopCommerce store and the ERP system respectively; both are optional and fail soft.
/// </summary>
public interface IErpReconciliationDataSource
{
    Task<ErpReconciliationTotals> GetLocalTotalsAsync(DateTime fromUtc, DateTime toUtc, CancellationToken cancellationToken);

    Task<ErpReconciliationTotals> GetErpTotalsAsync(DateTime fromUtc, DateTime toUtc, CancellationToken cancellationToken);
}
