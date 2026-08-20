using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace TwinParticles.CheckEngine.Domain.Tenancy;

/// <summary>
/// FR-1321: invoicing stays behind a port. Check Engine emits usage and can export rollups; no
/// billing vendor is hardcoded.
/// </summary>
public interface ITenantBillingAdapter
{
    Task<IReadOnlyList<TenantUsageDaily>> ExportUsageAsync(
        int tenantId,
        DateTime fromUtc,
        DateTime toUtc,
        CancellationToken cancellationToken);
}
