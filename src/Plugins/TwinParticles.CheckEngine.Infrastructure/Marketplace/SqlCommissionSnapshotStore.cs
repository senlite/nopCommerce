using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LinqToDB.Data;
using Nop.Data;
using TwinParticles.CheckEngine.Domain.Marketplace;
using TwinParticles.CheckEngine.Infrastructure.Data;

namespace TwinParticles.CheckEngine.Infrastructure.Marketplace;

public sealed class SqlCommissionSnapshotStore : ICommissionSnapshotStore
{
    private readonly INopDataProvider _dataProvider;

    public SqlCommissionSnapshotStore(INopDataProvider dataProvider)
    {
        _dataProvider = dataProvider;
    }

    public async Task SaveSnapshotsAsync(IReadOnlyCollection<OrderLineCommissionSnapshot> snapshots, CancellationToken cancellationToken)
    {
        foreach (var snapshot in snapshots)
        {
            await _dataProvider.ExecuteNonQueryAsync(@"
INSERT INTO TP_CE_OrderLineCommissionSnapshot
(OrderId, OrderItemId, VendorId, RuleId, ModelKindId, BasisId, RateApplied, CommissionAmount, LineSubtotalExclTax, Quantity, SnapshottedUtc)
VALUES
(@orderId, @orderItemId, @vendorId, @ruleId, @modelKindId, @basisId, @rateApplied, @commissionAmount, @lineSubtotal, @quantity, @snapshottedUtc)",
                new DataParameter("orderId", snapshot.OrderId),
                new DataParameter("orderItemId", snapshot.OrderItemId),
                new DataParameter("vendorId", snapshot.VendorId),
                new DataParameter("ruleId", snapshot.RuleId),
                new DataParameter("modelKindId", (int)snapshot.ModelKind),
                new DataParameter("basisId", (int)snapshot.Basis),
                new DataParameter("rateApplied", snapshot.RateApplied),
                new DataParameter("commissionAmount", snapshot.CommissionAmount),
                new DataParameter("lineSubtotal", snapshot.LineSubtotalExclTax),
                new DataParameter("quantity", snapshot.Quantity),
                new DataParameter("snapshottedUtc", snapshot.SnapshottedUtc));
        }
    }

    public async Task<IReadOnlyList<OrderLineCommissionSnapshot>> GetByOrderIdAsync(int orderId, CancellationToken cancellationToken)
    {
        var rows = await _dataProvider.QueryAsync<SnapshotRow>(@"
SELECT Id, OrderId, OrderItemId, VendorId, RuleId, ModelKindId, BasisId, RateApplied, CommissionAmount,
       LineSubtotalExclTax, Quantity, SnapshottedUtc
FROM TP_CE_OrderLineCommissionSnapshot
WHERE OrderId = @orderId
ORDER BY OrderItemId",
            new DataParameter("orderId", orderId));

        return rows.Select(row => new OrderLineCommissionSnapshot
        {
            Id = row.Id,
            OrderId = row.OrderId,
            OrderItemId = row.OrderItemId,
            VendorId = row.VendorId,
            RuleId = row.RuleId,
            ModelKind = (CommissionModelKind)row.ModelKindId,
            Basis = (CommissionBasis)row.BasisId,
            RateApplied = row.RateApplied,
            CommissionAmount = row.CommissionAmount,
            LineSubtotalExclTax = row.LineSubtotalExclTax,
            Quantity = row.Quantity,
            SnapshottedUtc = row.SnapshottedUtc
        }).ToList();
    }

    public async Task<decimal> GetVendorMonthVolumeExclTaxAsync(
        int vendorId,
        int year,
        int month,
        int excludeOrderId,
        CancellationToken cancellationToken)
    {
        var start = new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Utc);
        var end = start.AddMonths(1);
        var orderTable = CheckEngineSql.QuoteIdentifier("Order");

        var rows = await _dataProvider.QueryAsync<VolumeRow>($@"
SELECT COALESCE(SUM(s.LineSubtotalExclTax), 0) AS Volume
FROM TP_CE_OrderLineCommissionSnapshot s
INNER JOIN {orderTable} o ON o.Id = s.OrderId
WHERE s.VendorId = @vendorId
  AND s.OrderId <> @excludeOrderId
  AND o.Deleted = 0
  AND o.CreatedOnUtc >= @startUtc
  AND o.CreatedOnUtc < @endUtc",
            new DataParameter("vendorId", vendorId),
            new DataParameter("excludeOrderId", excludeOrderId),
            new DataParameter("startUtc", start),
            new DataParameter("endUtc", end));

        return rows.FirstOrDefault()?.Volume ?? 0m;
    }

    private sealed class SnapshotRow
    {
        public int Id { get; set; }
        public int OrderId { get; set; }
        public int OrderItemId { get; set; }
        public int VendorId { get; set; }
        public int RuleId { get; set; }
        public int ModelKindId { get; set; }
        public int BasisId { get; set; }
        public decimal RateApplied { get; set; }
        public decimal CommissionAmount { get; set; }
        public decimal LineSubtotalExclTax { get; set; }
        public int Quantity { get; set; }
        public DateTime SnapshottedUtc { get; set; }
    }

    private sealed class VolumeRow
    {
        public decimal Volume { get; set; }
    }
}
