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

public sealed class SqlPayoutStatementRepository : IPayoutStatementRepository
{
    private readonly INopDataProvider _dataProvider;

    public SqlPayoutStatementRepository(INopDataProvider dataProvider)
    {
        _dataProvider = dataProvider;
    }

    public async Task<int> InsertAsync(PayoutStatement statement, CancellationToken cancellationToken)
    {
        var inserted = await _dataProvider.QueryAsync<ScalarIntRow>(@"
INSERT INTO TP_CE_PayoutStatement
(VendorId, PeriodStartUtc, PeriodEndUtc, GrossSales, TotalCommission, TotalRefunds, TotalAdjustments, NetPayout, StatusId, ErpReferenceId, ErpReportedTotal, CreatedUtc, FinalizedUtc)
VALUES
(@vendorId, @periodStartUtc, @periodEndUtc, @grossSales, @totalCommission, @totalRefunds, @totalAdjustments, @netPayout, @statusId, @erpReferenceId, @erpReportedTotal, @createdUtc, @finalizedUtc);
" + CheckEngineSql.SelectInsertedIntId(),
            new DataParameter("vendorId", statement.VendorId),
            new DataParameter("periodStartUtc", statement.PeriodStartUtc),
            new DataParameter("periodEndUtc", statement.PeriodEndUtc),
            new DataParameter("grossSales", statement.GrossSales),
            new DataParameter("totalCommission", statement.TotalCommission),
            new DataParameter("totalRefunds", statement.TotalRefunds),
            new DataParameter("totalAdjustments", statement.TotalAdjustments),
            new DataParameter("netPayout", statement.NetPayout),
            new DataParameter("statusId", (int)statement.Status),
            new DataParameter("erpReferenceId", statement.ErpReferenceId),
            new DataParameter("erpReportedTotal", statement.ErpReportedTotal),
            new DataParameter("createdUtc", statement.CreatedUtc),
            new DataParameter("finalizedUtc", statement.FinalizedUtc));

        statement.Id = inserted.Single().Value;

        foreach (var line in statement.Lines)
        {
            await _dataProvider.ExecuteNonQueryAsync(@"
INSERT INTO TP_CE_PayoutStatementLine
(StatementId, OrderId, OrderItemId, LineSubtotalExclTax, CommissionAmount, RefundAmount)
VALUES
(@statementId, @orderId, @orderItemId, @lineSubtotal, @commissionAmount, @refundAmount)",
                new DataParameter("statementId", statement.Id),
                new DataParameter("orderId", line.OrderId),
                new DataParameter("orderItemId", line.OrderItemId),
                new DataParameter("lineSubtotal", line.LineSubtotalExclTax),
                new DataParameter("commissionAmount", line.CommissionAmount),
                new DataParameter("refundAmount", line.RefundAmount));
        }

        return statement.Id;
    }

    public Task UpdateAsync(PayoutStatement statement, CancellationToken cancellationToken)
        => _dataProvider.ExecuteNonQueryAsync(@"
UPDATE TP_CE_PayoutStatement
SET StatusId = @statusId, ErpReferenceId = @erpReferenceId, ErpReportedTotal = @erpReportedTotal, FinalizedUtc = @finalizedUtc
WHERE Id = @id",
            new DataParameter("statusId", (int)statement.Status),
            new DataParameter("erpReferenceId", statement.ErpReferenceId),
            new DataParameter("erpReportedTotal", statement.ErpReportedTotal),
            new DataParameter("finalizedUtc", statement.FinalizedUtc),
            new DataParameter("id", statement.Id));

    public async Task<PayoutStatement?> GetByIdAsync(int statementId, CancellationToken cancellationToken)
    {
        var rows = await _dataProvider.QueryAsync<StatementRow>(@"
SELECT Id, VendorId, PeriodStartUtc, PeriodEndUtc, GrossSales, TotalCommission, TotalRefunds, TotalAdjustments,
       NetPayout, StatusId, ErpReferenceId, ErpReportedTotal, CreatedUtc, FinalizedUtc
FROM TP_CE_PayoutStatement
WHERE Id = @id",
            new DataParameter("id", statementId));

        var row = rows.FirstOrDefault();
        if (row is null)
            return null;

        var lines = await _dataProvider.QueryAsync<LineRow>(@"
SELECT Id, StatementId, OrderId, OrderItemId, LineSubtotalExclTax, CommissionAmount, RefundAmount
FROM TP_CE_PayoutStatementLine
WHERE StatementId = @statementId
ORDER BY OrderId, OrderItemId",
            new DataParameter("statementId", statementId));

        return MapStatement(row, lines);
    }

    public async Task<IReadOnlyList<PayoutStatement>> ListByVendorAsync(int vendorId, CancellationToken cancellationToken)
    {
        var rows = await _dataProvider.QueryAsync<StatementRow>(@"
SELECT Id, VendorId, PeriodStartUtc, PeriodEndUtc, GrossSales, TotalCommission, TotalRefunds, TotalAdjustments,
       NetPayout, StatusId, ErpReferenceId, ErpReportedTotal, CreatedUtc, FinalizedUtc
FROM TP_CE_PayoutStatement
WHERE VendorId = @vendorId
ORDER BY CreatedUtc DESC",
            new DataParameter("vendorId", vendorId));

        var results = new List<PayoutStatement>();
        foreach (var row in rows)
            results.Add(MapStatement(row, []));

        return results;
    }

    public async Task<PayoutStatement?> GetLatestByVendorAsync(int vendorId, CancellationToken cancellationToken)
    {
        var rows = await _dataProvider.QueryAsync<StatementRow>(CheckEngineSql.SelectTop(
            1,
            "Id, VendorId, PeriodStartUtc, PeriodEndUtc, GrossSales, TotalCommission, TotalRefunds, TotalAdjustments, NetPayout, StatusId, ErpReferenceId, ErpReportedTotal, CreatedUtc, FinalizedUtc",
            "FROM TP_CE_PayoutStatement WHERE VendorId = @vendorId ORDER BY CreatedUtc DESC"),
            new DataParameter("vendorId", vendorId));

        var row = rows.FirstOrDefault();
        return row is null ? null : MapStatement(row, []);
    }

    public async Task InsertAdjustmentAsync(PayoutAdjustment adjustment, CancellationToken cancellationToken)
    {
        var inserted = await _dataProvider.QueryAsync<ScalarIntRow>(@"
INSERT INTO TP_CE_PayoutAdjustment (VendorId, StatementId, Amount, ReasonCode, Notes, CreatedBy, CreatedUtc)
VALUES (@vendorId, @statementId, @amount, @reasonCode, @notes, @createdBy, @createdUtc);
" + CheckEngineSql.SelectInsertedIntId(),
            new DataParameter("vendorId", adjustment.VendorId),
            new DataParameter("statementId", adjustment.StatementId),
            new DataParameter("amount", adjustment.Amount),
            new DataParameter("reasonCode", adjustment.ReasonCode),
            new DataParameter("notes", adjustment.Notes),
            new DataParameter("createdBy", adjustment.CreatedBy),
            new DataParameter("createdUtc", adjustment.CreatedUtc));

        adjustment.Id = inserted.Single().Value;
    }

    public async Task<IReadOnlyList<PayoutAdjustment>> GetPendingAdjustmentsAsync(int vendorId, CancellationToken cancellationToken)
    {
        var rows = await _dataProvider.QueryAsync<AdjustmentRow>(@"
SELECT Id, VendorId, StatementId, Amount, ReasonCode, Notes, CreatedBy, CreatedUtc
FROM TP_CE_PayoutAdjustment
WHERE VendorId = @vendorId AND StatementId IS NULL
ORDER BY CreatedUtc",
            new DataParameter("vendorId", vendorId));

        return rows.Select(MapAdjustment).ToList();
    }

    public Task AssignAdjustmentsToStatementAsync(int vendorId, int statementId, CancellationToken cancellationToken)
        => _dataProvider.ExecuteNonQueryAsync(@"
UPDATE TP_CE_PayoutAdjustment
SET StatementId = @statementId
WHERE VendorId = @vendorId AND StatementId IS NULL",
            new DataParameter("statementId", statementId),
            new DataParameter("vendorId", vendorId));

    private static PayoutStatement MapStatement(StatementRow row, IEnumerable<LineRow> lines)
        => new()
        {
            Id = row.Id,
            VendorId = row.VendorId,
            PeriodStartUtc = row.PeriodStartUtc,
            PeriodEndUtc = row.PeriodEndUtc,
            GrossSales = row.GrossSales,
            TotalCommission = row.TotalCommission,
            TotalRefunds = row.TotalRefunds,
            TotalAdjustments = row.TotalAdjustments,
            NetPayout = row.NetPayout,
            Status = (PayoutStatementStatus)row.StatusId,
            ErpReferenceId = row.ErpReferenceId,
            ErpReportedTotal = row.ErpReportedTotal,
            CreatedUtc = row.CreatedUtc,
            FinalizedUtc = row.FinalizedUtc,
            Lines = lines.Select(line => new PayoutStatementLine
            {
                Id = line.Id,
                StatementId = line.StatementId,
                OrderId = line.OrderId,
                OrderItemId = line.OrderItemId,
                LineSubtotalExclTax = line.LineSubtotalExclTax,
                CommissionAmount = line.CommissionAmount,
                RefundAmount = line.RefundAmount
            }).ToList()
        };

    private static PayoutAdjustment MapAdjustment(AdjustmentRow row)
        => new()
        {
            Id = row.Id,
            VendorId = row.VendorId,
            StatementId = row.StatementId,
            Amount = row.Amount,
            ReasonCode = row.ReasonCode,
            Notes = row.Notes,
            CreatedBy = row.CreatedBy,
            CreatedUtc = row.CreatedUtc
        };

    private sealed class StatementRow
    {
        public int Id { get; set; }
        public int VendorId { get; set; }
        public DateTime PeriodStartUtc { get; set; }
        public DateTime PeriodEndUtc { get; set; }
        public decimal GrossSales { get; set; }
        public decimal TotalCommission { get; set; }
        public decimal TotalRefunds { get; set; }
        public decimal TotalAdjustments { get; set; }
        public decimal NetPayout { get; set; }
        public int StatusId { get; set; }
        public string? ErpReferenceId { get; set; }
        public decimal? ErpReportedTotal { get; set; }
        public DateTime CreatedUtc { get; set; }
        public DateTime? FinalizedUtc { get; set; }
    }

    private sealed class LineRow
    {
        public int Id { get; set; }
        public int StatementId { get; set; }
        public int OrderId { get; set; }
        public int OrderItemId { get; set; }
        public decimal LineSubtotalExclTax { get; set; }
        public decimal CommissionAmount { get; set; }
        public decimal RefundAmount { get; set; }
    }

    private sealed class AdjustmentRow
    {
        public int Id { get; set; }
        public int VendorId { get; set; }
        public int? StatementId { get; set; }
        public decimal Amount { get; set; }
        public string ReasonCode { get; set; } = string.Empty;
        public string? Notes { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
        public DateTime CreatedUtc { get; set; }
    }

    private sealed class ScalarIntRow
    {
        public int Value { get; set; }
    }
}
