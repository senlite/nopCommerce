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

public sealed class SqlOrderVendorSplitStore : IOrderVendorSplitStore
{
    private readonly INopDataProvider _dataProvider;

    public SqlOrderVendorSplitStore(INopDataProvider dataProvider)
    {
        _dataProvider = dataProvider;
    }

    public async Task<bool> ExistsForOrderAsync(int orderId, CancellationToken cancellationToken)
    {
        var count = await _dataProvider.QueryAsync<int>(@"
SELECT COUNT(1)
FROM TP_CE_OrderVendorSplit
WHERE ParentOrderId = @orderId",
            new DataParameter("orderId", orderId));

        return count.FirstOrDefault() > 0;
    }

    public async Task SaveCheckoutGroupAsync(OrderCheckoutGroup group, CancellationToken cancellationToken)
    {
        foreach (var split in group.Splits)
        {
            var splitId = await _dataProvider.QueryAsync<int>(@"
INSERT INTO TP_CE_OrderVendorSplit
(CheckoutGroupId, ParentOrderId, VendorId, LineSubtotalExclTax, CreatedUtc)
VALUES
(@checkoutGroupId, @parentOrderId, @vendorId, @lineSubtotal, @createdUtc);
" + CheckEngineSql.SelectInsertedIntId(),
                new DataParameter("checkoutGroupId", group.CheckoutGroupId),
                new DataParameter("parentOrderId", group.ParentOrderId),
                new DataParameter("vendorId", split.VendorId),
                new DataParameter("lineSubtotal", split.LineSubtotalExclTax),
                new DataParameter("createdUtc", group.CreatedUtc));

            var id = splitId.FirstOrDefault();
            foreach (var line in split.Lines)
            {
                await _dataProvider.ExecuteNonQueryAsync(@"
INSERT INTO TP_CE_OrderVendorSplitLine
(SplitId, OrderItemId, ProductId, Quantity, LineSubtotalExclTax)
VALUES
(@splitId, @orderItemId, @productId, @quantity, @lineSubtotal)",
                    new DataParameter("splitId", id),
                    new DataParameter("orderItemId", line.OrderItemId),
                    new DataParameter("productId", line.ProductId),
                    new DataParameter("quantity", line.Quantity),
                    new DataParameter("lineSubtotal", line.LineSubtotalExclTax));
            }
        }
    }

    public async Task<OrderCheckoutGroup?> GetByOrderIdAsync(int orderId, CancellationToken cancellationToken)
    {
        var splitRows = await _dataProvider.QueryAsync<SplitRow>(@"
SELECT Id, CheckoutGroupId, ParentOrderId, VendorId, LineSubtotalExclTax, CreatedUtc
FROM TP_CE_OrderVendorSplit
WHERE ParentOrderId = @orderId
ORDER BY VendorId",
            new DataParameter("orderId", orderId));

        var rows = splitRows.ToList();
        if (rows.Count == 0)
            return null;

        var first = rows[0];
        var splitIds = rows.Select(row => row.Id).ToList();
        var inList = string.Join(",", splitIds);
        var lineRows = await _dataProvider.QueryAsync<SplitLineRow>($@"
SELECT Id, SplitId, OrderItemId, ProductId, Quantity, LineSubtotalExclTax
FROM TP_CE_OrderVendorSplitLine
WHERE SplitId IN ({inList})
ORDER BY SplitId, OrderItemId");

        var linesBySplit = lineRows
            .GroupBy(line => line.SplitId)
            .ToDictionary(
                group => group.Key,
                group => group.Select(line => new OrderVendorSplitLine
                {
                    Id = line.Id,
                    SplitId = line.SplitId,
                    OrderItemId = line.OrderItemId,
                    ProductId = line.ProductId,
                    Quantity = line.Quantity,
                    LineSubtotalExclTax = line.LineSubtotalExclTax
                }).ToList());

        return new OrderCheckoutGroup
        {
            CheckoutGroupId = first.CheckoutGroupId,
            ParentOrderId = first.ParentOrderId,
            CreatedUtc = first.CreatedUtc,
            Splits = rows.Select(row => new OrderVendorSplit
            {
                Id = row.Id,
                CheckoutGroupId = row.CheckoutGroupId,
                ParentOrderId = row.ParentOrderId,
                VendorId = row.VendorId,
                LineSubtotalExclTax = row.LineSubtotalExclTax,
                Lines = linesBySplit.TryGetValue(row.Id, out var lines) ? lines : []
            }).ToList()
        };
    }

    public async Task<bool> ShipmentMapExistsAsync(int shipmentId, CancellationToken cancellationToken)
    {
        var count = await _dataProvider.QueryAsync<int>(@"
SELECT COUNT(1)
FROM TP_CE_ShipmentVendorMap
WHERE ShipmentId = @shipmentId",
            new DataParameter("shipmentId", shipmentId));

        return count.FirstOrDefault() > 0;
    }

    public async Task SaveShipmentVendorMapsAsync(IReadOnlyCollection<ShipmentVendorMap> maps, CancellationToken cancellationToken)
    {
        foreach (var map in maps)
        {
            await _dataProvider.ExecuteNonQueryAsync(@"
INSERT INTO TP_CE_ShipmentVendorMap
(ShipmentId, OrderId, VendorId, CreatedUtc)
VALUES
(@shipmentId, @orderId, @vendorId, @createdUtc)",
                new DataParameter("shipmentId", map.ShipmentId),
                new DataParameter("orderId", map.OrderId),
                new DataParameter("vendorId", map.VendorId),
                new DataParameter("createdUtc", map.CreatedUtc));
        }
    }

    public async Task<IReadOnlyList<ShipmentVendorMap>> GetShipmentMapsByOrderIdAsync(int orderId, CancellationToken cancellationToken)
    {
        var rows = await _dataProvider.QueryAsync<ShipmentMapRow>(@"
SELECT Id, ShipmentId, OrderId, VendorId, CreatedUtc
FROM TP_CE_ShipmentVendorMap
WHERE OrderId = @orderId
ORDER BY ShipmentId, VendorId",
            new DataParameter("orderId", orderId));

        return rows.Select(row => new ShipmentVendorMap
        {
            Id = row.Id,
            ShipmentId = row.ShipmentId,
            OrderId = row.OrderId,
            VendorId = row.VendorId,
            CreatedUtc = row.CreatedUtc
        }).ToList();
    }

    private sealed class SplitRow
    {
        public int Id { get; set; }
        public Guid CheckoutGroupId { get; set; }
        public int ParentOrderId { get; set; }
        public int VendorId { get; set; }
        public decimal LineSubtotalExclTax { get; set; }
        public DateTime CreatedUtc { get; set; }
    }

    private sealed class SplitLineRow
    {
        public int Id { get; set; }
        public int SplitId { get; set; }
        public int OrderItemId { get; set; }
        public int ProductId { get; set; }
        public int Quantity { get; set; }
        public decimal LineSubtotalExclTax { get; set; }
    }

    private sealed class ShipmentMapRow
    {
        public int Id { get; set; }
        public int ShipmentId { get; set; }
        public int OrderId { get; set; }
        public int VendorId { get; set; }
        public DateTime CreatedUtc { get; set; }
    }
}
