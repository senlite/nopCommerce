using System;
using System.Threading;
using System.Threading.Tasks;
using TwinParticles.CheckEngine.Application.Licensing;
using TwinParticles.CheckEngine.Domain.Marketplace;
using TwinParticles.CheckEngine.Domain.Security;

namespace TwinParticles.CheckEngine.Application.Marketplace;

public sealed class CommissionPlanAdminService
{
    private readonly ICommissionPlanRepository _plans;
    private readonly MarketplaceLicenceGate _licenceGate;
    private readonly ICheckEngineAuditService _auditService;

    public CommissionPlanAdminService(
        ICommissionPlanRepository plans,
        MarketplaceLicenceGate licenceGate,
        ICheckEngineAuditService auditService)
    {
        _plans = plans;
        _licenceGate = licenceGate;
        _auditService = auditService;
    }

    public async Task<CommissionPlan?> GetPlanAsync(int vendorId, CancellationToken cancellationToken)
    {
        if (!await _licenceGate.AllowsMarketplaceAsync(cancellationToken))
            return null;

        return await _plans.GetActivePlanAsync(vendorId, cancellationToken);
    }

    public async Task<CommissionPlan?> SavePlanAsync(CommissionPlan plan, CancellationToken cancellationToken)
    {
        if (!await _licenceGate.AllowsMarketplaceAsync(cancellationToken))
            return null;

        if (plan.VendorId <= 0)
            throw new ArgumentException("Vendor id must be positive.", nameof(plan));

        await _plans.UpsertPlanAsync(plan, cancellationToken);

        await _auditService.AppendAsync(
            "admin",
            "commission.plan.saved",
            "CommissionPlan",
            plan.VendorId.ToString(),
            beforeJson: null,
            afterJson: $"{{\"vendorId\":{plan.VendorId},\"ruleCount\":{plan.Rules.Count}}}",
            cancellationToken);

        return await _plans.GetActivePlanAsync(plan.VendorId, cancellationToken);
    }
}
