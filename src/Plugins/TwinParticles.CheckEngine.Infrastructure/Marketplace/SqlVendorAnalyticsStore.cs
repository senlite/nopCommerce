using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LinqToDB.Data;
using Nop.Core.Domain.Orders;
using Nop.Core.Domain.Shipping;
using Nop.Data;
using TwinParticles.CheckEngine.Domain.Fitment;
using TwinParticles.CheckEngine.Domain.Marketplace;
using TwinParticles.CheckEngine.Infrastructure.Data;

namespace TwinParticles.CheckEngine.Infrastructure.Marketplace;

public sealed class SqlVendorAnalyticsStore : IVendorAnalyticsStore
{
    private const int OnTimeShipmentSlaDays = 3;

    private readonly INopDataProvider _dataProvider;

    public SqlVendorAnalyticsStore(INopDataProvider dataProvider)
    {
        _dataProvider = dataProvider;
    }

    public async Task<VendorScorecard> GetScorecardAsync(
        int vendorId,
        string? vendorName,
        IReadOnlyCollection<int> productIds,
        CancellationToken cancellationToken)
    {
        if (productIds.Count == 0)
            return VendorScorecard.Empty(vendorId > 0 ? vendorId : null, vendorName);

        var orderMetrics = await GetOrderMetricsAsync(productIds, cancellationToken);
        var shipmentMetrics = await GetShipmentMetricsAsync(productIds, cancellationToken);
        var fitmentSummary = vendorId > 0
            ? await GetFitmentProposalSummaryAsync(vendorId, cancellationToken)
            : new VendorFitmentProposalSummary();

        var nonCancelled = orderMetrics.TotalOrders - orderMetrics.CancelledOrders;
        decimal? fillRate = nonCancelled > 0
            ? (decimal)orderMetrics.FulfilledOrders / nonCancelled
            : null;
        decimal? cancelRate = orderMetrics.TotalOrders > 0
            ? (decimal)orderMetrics.CancelledOrders / orderMetrics.TotalOrders
            : null;
        decimal? onTimeRate = shipmentMetrics.TotalShipments > 0
            ? (decimal)shipmentMetrics.OnTimeShipments / shipmentMetrics.TotalShipments
            : null;

        var fitmentTotal = fitmentSummary.PendingReview + fitmentSummary.Published + fitmentSummary.Rejected;
        decimal? rejectionRate = fitmentTotal > 0
            ? (decimal)fitmentSummary.Rejected / fitmentTotal
            : null;

        return new VendorScorecard
        {
            VendorId = vendorId > 0 ? vendorId : null,
            VendorName = vendorName,
            FillRate = fillRate,
            CancelRate = cancelRate,
            ClaimRejectionRate = rejectionRate,
            OnTimeShipmentRate = onTimeRate,
            OrderSampleSize = orderMetrics.TotalOrders,
            FitmentClaimSampleSize = fitmentTotal,
            ShipmentSampleSize = shipmentMetrics.TotalShipments
        };
    }

    public async Task<VendorFitmentProposalSummary> GetFitmentProposalSummaryAsync(
        int vendorId,
        CancellationToken cancellationToken)
    {
        var prefix = VendorSourceReference.ForVendor(vendorId);
        var rows = await _dataProvider.QueryAsync<FitmentCountRow>(@"
SELECT FitmentStatusId, IsPublished, COUNT(*) AS ClaimCount
FROM TP_CE_FitmentClaim
WHERE VendorId = @vendorId OR SourceReference = @prefix
GROUP BY FitmentStatusId, IsPublished",
            new DataParameter("vendorId", vendorId),
            new DataParameter("prefix", prefix));

        var pending = 0;
        var published = 0;
        var rejected = 0;

        foreach (var row in rows)
        {
            if (row.FitmentStatusId == (int)FitmentStatus.Rejected)
                rejected += row.ClaimCount;
            else if (row.IsPublished)
                published += row.ClaimCount;
            else
                pending += row.ClaimCount;
        }

        return new VendorFitmentProposalSummary
        {
            PendingReview = pending,
            Published = published,
            Rejected = rejected
        };
    }

    private async Task<OrderMetricsRow> GetOrderMetricsAsync(
        IReadOnlyCollection<int> productIds,
        CancellationToken cancellationToken)
    {
        var orderTable = CheckEngineSql.QuoteIdentifier("Order");
        var inList = string.Join(",", productIds.Select(id => id.ToString()));
        var complete = (int)OrderStatus.Complete;
        var cancelled = (int)OrderStatus.Cancelled;
        var shipped = (int)ShippingStatus.Shipped;
        var delivered = (int)ShippingStatus.Delivered;

        var rows = await _dataProvider.QueryAsync<OrderMetricsRow>($@"
SELECT
    COUNT(DISTINCT o.Id) AS TotalOrders,
    COUNT(DISTINCT CASE WHEN o.OrderStatusId = {cancelled} THEN o.Id END) AS CancelledOrders,
    COUNT(DISTINCT CASE
        WHEN o.OrderStatusId = {complete}
          OR o.ShippingStatusId IN ({shipped}, {delivered})
        THEN o.Id END) AS FulfilledOrders
FROM {orderTable} o
INNER JOIN OrderItem oi ON oi.OrderId = o.Id
WHERE o.Deleted = 0 AND oi.ProductId IN ({inList})");

        return rows.FirstOrDefault() ?? new OrderMetricsRow();
    }

    private async Task<ShipmentMetricsRow> GetShipmentMetricsAsync(
        IReadOnlyCollection<int> productIds,
        CancellationToken cancellationToken)
    {
        var orderTable = CheckEngineSql.QuoteIdentifier("Order");
        var inList = string.Join(",", productIds.Select(id => id.ToString()));
        var slaDays = OnTimeShipmentSlaDays;

        var rows = await _dataProvider.QueryAsync<ShipmentMetricsRow>($@"
SELECT
    COUNT(DISTINCT s.Id) AS TotalShipments,
    COUNT(DISTINCT CASE
        WHEN s.ShippedDateUtc IS NOT NULL
         AND s.ShippedDateUtc <= {CheckEngineSql.DateAddDays("o.CreatedOnUtc", slaDays)}
        THEN s.Id END) AS OnTimeShipments
FROM Shipment s
INNER JOIN {orderTable} o ON o.Id = s.OrderId
INNER JOIN OrderItem oi ON oi.OrderId = o.Id
WHERE o.Deleted = 0 AND oi.ProductId IN ({inList})");

        return rows.FirstOrDefault() ?? new ShipmentMetricsRow();
    }

    private sealed class OrderMetricsRow
    {
        public int TotalOrders { get; set; }
        public int CancelledOrders { get; set; }
        public int FulfilledOrders { get; set; }
    }

    private sealed class ShipmentMetricsRow
    {
        public int TotalShipments { get; set; }
        public int OnTimeShipments { get; set; }
    }

    private sealed class FitmentCountRow
    {
        public int FitmentStatusId { get; set; }
        public bool IsPublished { get; set; }
        public int ClaimCount { get; set; }
    }
}
