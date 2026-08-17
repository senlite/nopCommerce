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

public sealed class SqlPayoutStatementDataSource : IPayoutStatementDataSource
{
    private readonly INopDataProvider _dataProvider;

    public SqlPayoutStatementDataSource(INopDataProvider dataProvider)
    {
        _dataProvider = dataProvider;
    }

    public async Task<IReadOnlyList<PayoutSourceLine>> GetVendorLinesAsync(
        int vendorId,
        DateTime periodStartUtc,
        DateTime periodEndUtc,
        CancellationToken cancellationToken)
    {
        var orderTable = CheckEngineSql.QuoteIdentifier("Order");
        var rows = await _dataProvider.QueryAsync<SourceRow>($@"
SELECT s.OrderId,
       s.OrderItemId,
       s.LineSubtotalExclTax,
       s.CommissionAmount,
       o.CreatedOnUtc AS OrderCreatedUtc,
       CASE
           WHEN o.OrderSubtotalExclTax > 0 AND o.RefundedAmount > 0
           THEN s.LineSubtotalExclTax / o.OrderSubtotalExclTax * o.RefundedAmount
           ELSE 0
       END AS RefundAmount
FROM TP_CE_OrderLineCommissionSnapshot s
INNER JOIN {orderTable} o ON o.Id = s.OrderId
WHERE s.VendorId = @vendorId
  AND o.Deleted = 0
  AND o.CreatedOnUtc >= @periodStartUtc
  AND o.CreatedOnUtc < @periodEndUtc
ORDER BY s.OrderId, s.OrderItemId",
            new DataParameter("vendorId", vendorId),
            new DataParameter("periodStartUtc", periodStartUtc),
            new DataParameter("periodEndUtc", periodEndUtc));

        return rows.Select(row => new PayoutSourceLine
        {
            OrderId = row.OrderId,
            OrderItemId = row.OrderItemId,
            LineSubtotalExclTax = row.LineSubtotalExclTax,
            CommissionAmount = row.CommissionAmount,
            RefundAmount = row.RefundAmount,
            OrderCreatedUtc = row.OrderCreatedUtc
        }).ToList();
    }

    private sealed class SourceRow
    {
        public int OrderId { get; set; }
        public int OrderItemId { get; set; }
        public decimal LineSubtotalExclTax { get; set; }
        public decimal CommissionAmount { get; set; }
        public decimal RefundAmount { get; set; }
        public DateTime OrderCreatedUtc { get; set; }
    }
}
