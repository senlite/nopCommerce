using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Nop.Data;
using TwinParticles.CheckEngine.Domain.Marketplace;
using TwinParticles.CheckEngine.Infrastructure.Data;

namespace TwinParticles.CheckEngine.Infrastructure.Marketplace;

public sealed class SqlVendorOrderReadStore : IVendorOrderReadStore
{
    private readonly INopDataProvider _dataProvider;

    public SqlVendorOrderReadStore(INopDataProvider dataProvider)
    {
        _dataProvider = dataProvider;
    }

    public async Task<IReadOnlyList<VendorOrderRecord>> GetOrdersForProductsAsync(
        IReadOnlyCollection<int> productIds,
        CancellationToken cancellationToken)
    {
        if (productIds.Count == 0)
            return [];

        var orderTable = CheckEngineSql.QuoteIdentifier("Order");
        var inList = string.Join(",", productIds.Select(id => id.ToString()));
        var rows = await _dataProvider.QueryAsync<OrderRow>($@"
SELECT o.Id AS OrderId, o.CustomerId, oi.ProductId
FROM {orderTable} o
INNER JOIN OrderItem oi ON oi.OrderId = o.Id
WHERE o.Deleted = 0 AND oi.ProductId IN ({inList})
ORDER BY o.Id, oi.ProductId");

        return rows
            .GroupBy(row => new { row.OrderId, row.CustomerId })
            .Select(group => new VendorOrderRecord
            {
                OrderId = group.Key.OrderId,
                CustomerId = group.Key.CustomerId,
                ProductIds = group.Select(row => row.ProductId).Distinct().ToList()
            })
            .ToList();
    }

    private sealed class OrderRow
    {
        public int OrderId { get; set; }
        public int CustomerId { get; set; }
        public int ProductId { get; set; }
    }
}
