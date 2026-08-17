using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TwinParticles.CheckEngine.Application.Licensing;
using TwinParticles.CheckEngine.Domain.Marketplace;

namespace TwinParticles.CheckEngine.Application.Marketplace;

public sealed class CommissionSnapshotService
{
    private readonly CommissionEvaluationService _evaluation;
    private readonly ICommissionPlanRepository _plans;
    private readonly ICommissionSnapshotStore _snapshots;
    private readonly IVendorOwnershipStore _ownership;
    private readonly IVendorRepository _vendors;
    private readonly IVendorProductCategoryStore _categories;
    private readonly MarketplaceLicenceGate _licenceGate;

    public CommissionSnapshotService(
        CommissionEvaluationService evaluation,
        ICommissionPlanRepository plans,
        ICommissionSnapshotStore snapshots,
        IVendorOwnershipStore ownership,
        IVendorRepository vendors,
        IVendorProductCategoryStore categories,
        MarketplaceLicenceGate licenceGate)
    {
        _evaluation = evaluation;
        _plans = plans;
        _snapshots = snapshots;
        _ownership = ownership;
        _vendors = vendors;
        _categories = categories;
        _licenceGate = licenceGate;
    }

    public async Task<IReadOnlyList<OrderLineCommissionSnapshot>> SnapshotOrderAsync(
        int orderId,
        DateTime orderCreatedUtc,
        IReadOnlyList<VendorOrderLine> items,
        CancellationToken cancellationToken)
    {
        if (!await _licenceGate.AllowsMarketplaceAsync(cancellationToken) || items.Count == 0)
            return [];

        var productIds = items.Select(item => item.ProductId).Distinct().ToList();
        var categoryMap = await _categories.GetCategoryIdsByProductIdsAsync(productIds, cancellationToken);
        var orderSubtotal = items.Sum(item => item.PriceExclTax);
        var vendorFlatApplied = new HashSet<int>();
        var results = new List<OrderLineCommissionSnapshot>();
        var snapshottedUtc = DateTime.UtcNow;

        foreach (var item in items)
        {
            var vendorId = await ResolveVendorIdAsync(item.ProductId, cancellationToken);
            if (!vendorId.HasValue)
                continue;

            var plan = await _plans.GetActivePlanAsync(vendorId.Value, cancellationToken);
            if (plan is null || plan.Rules.Count == 0)
                continue;

            var volume = await _snapshots.GetVendorMonthVolumeExclTaxAsync(
                vendorId.Value,
                orderCreatedUtc.Year,
                orderCreatedUtc.Month,
                orderId,
                cancellationToken);

            categoryMap.TryGetValue(item.ProductId, out var categoryIds);

            var evaluation = _evaluation.EvaluateLine(plan, new CommissionLineContext
            {
                VendorId = vendorId.Value,
                ProductId = item.ProductId,
                CategoryIds = categoryIds ?? [],
                Quantity = item.Quantity,
                LineSubtotalExclTax = item.PriceExclTax,
                OrderSubtotalExclTax = orderSubtotal,
                VendorPeriodVolumeExclTax = volume,
                OrderUtc = orderCreatedUtc,
                FlatPerOrderAlreadyApplied = vendorFlatApplied.Contains(vendorId.Value)
            });

            if (!evaluation.Matched)
                continue;

            if (evaluation.Basis == CommissionBasis.PerOrder)
                vendorFlatApplied.Add(vendorId.Value);

            results.Add(new OrderLineCommissionSnapshot
            {
                OrderId = orderId,
                OrderItemId = item.OrderItemId,
                VendorId = vendorId.Value,
                RuleId = evaluation.RuleId,
                ModelKind = evaluation.ModelKind,
                Basis = evaluation.Basis,
                RateApplied = evaluation.RateApplied,
                CommissionAmount = evaluation.CommissionAmount,
                LineSubtotalExclTax = item.PriceExclTax,
                Quantity = item.Quantity,
                SnapshottedUtc = snapshottedUtc
            });
        }

        if (results.Count > 0)
            await _snapshots.SaveSnapshotsAsync(results, cancellationToken);

        return results;
    }

    public Task<IReadOnlyList<OrderLineCommissionSnapshot>> GetSnapshotsAsync(int orderId, CancellationToken cancellationToken)
        => _snapshots.GetByOrderIdAsync(orderId, cancellationToken);

    private async Task<int?> ResolveVendorIdAsync(int productId, CancellationToken cancellationToken)
    {
        var vendorId = await _ownership.GetProductVendorIdAsync(productId, cancellationToken);
        if (vendorId.HasValue)
            return vendorId;

        var operatorVendor = await _vendors.GetOperatorAsync(cancellationToken);
        return operatorVendor?.Id;
    }
}
