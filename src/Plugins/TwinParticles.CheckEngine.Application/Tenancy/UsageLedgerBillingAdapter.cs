using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TwinParticles.CheckEngine.Domain.Tenancy;

namespace TwinParticles.CheckEngine.Application.Tenancy;

public sealed class UsageLedgerBillingAdapter : ITenantBillingAdapter
{
    private readonly ITenantUsageLedger _ledger;

    public UsageLedgerBillingAdapter(ITenantUsageLedger ledger)
    {
        _ledger = ledger;
    }

    public Task<IReadOnlyList<TenantUsageDaily>> ExportUsageAsync(
        int tenantId,
        DateTime fromUtc,
        DateTime toUtc,
        CancellationToken cancellationToken)
        => _ledger.ListDailyAsync(tenantId, fromUtc, toUtc, cancellationToken);
}
