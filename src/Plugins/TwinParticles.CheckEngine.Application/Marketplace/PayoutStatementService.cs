using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using TwinParticles.CheckEngine.Application.Erp;
using TwinParticles.CheckEngine.Application.Licensing;
using TwinParticles.CheckEngine.Domain.Erp;
using TwinParticles.CheckEngine.Domain.Marketplace;
using TwinParticles.CheckEngine.Domain.Security;

namespace TwinParticles.CheckEngine.Application.Marketplace;

public sealed class PayoutStatementService
{
    private readonly PayoutStatementBuilder _builder;
    private readonly IPayoutStatementRepository _repository;
    private readonly IPayoutStatementDataSource _dataSource;
    private readonly ErpSyncService _erpSync;
    private readonly MarketplaceLicenceGate _licenceGate;
    private readonly ICheckEngineAuditService _auditService;

    public PayoutStatementService(
        PayoutStatementBuilder builder,
        IPayoutStatementRepository repository,
        IPayoutStatementDataSource dataSource,
        ErpSyncService erpSync,
        MarketplaceLicenceGate licenceGate,
        ICheckEngineAuditService auditService)
    {
        _builder = builder;
        _repository = repository;
        _dataSource = dataSource;
        _erpSync = erpSync;
        _licenceGate = licenceGate;
        _auditService = auditService;
    }

    public async Task<PayoutStatement?> GenerateAsync(
        int vendorId,
        DateTime periodStartUtc,
        DateTime periodEndUtc,
        CancellationToken cancellationToken)
    {
        if (!await _licenceGate.AllowsMarketplaceAsync(cancellationToken))
            return null;

        var sourceLines = await _dataSource.GetVendorLinesAsync(vendorId, periodStartUtc, periodEndUtc, cancellationToken);
        var adjustments = await _repository.GetPendingAdjustmentsAsync(vendorId, cancellationToken);
        var draft = _builder.Build(vendorId, periodStartUtc, periodEndUtc, sourceLines, adjustments);

        draft.Id = await _repository.InsertAsync(draft, cancellationToken);
        await _repository.AssignAdjustmentsToStatementAsync(vendorId, draft.Id, cancellationToken);

        await _auditService.AppendAsync(
            "admin",
            "payout.statement.generated",
            "PayoutStatement",
            draft.Id.ToString(),
            beforeJson: null,
            afterJson: $"{{\"vendorId\":{vendorId},\"netPayout\":{draft.NetPayout}}}",
            cancellationToken);

        return await _repository.GetByIdAsync(draft.Id, cancellationToken);
    }

    public async Task<PayoutStatement?> FinalizeAsync(int statementId, CancellationToken cancellationToken)
    {
        if (!await _licenceGate.AllowsMarketplaceAsync(cancellationToken))
            return null;

        var statement = await _repository.GetByIdAsync(statementId, cancellationToken);
        if (statement is null || statement.Status != PayoutStatementStatus.Draft)
            return statement;

        statement.Status = PayoutStatementStatus.Finalized;
        statement.FinalizedUtc = DateTime.UtcNow;
        await _repository.UpdateAsync(statement, cancellationToken);

        await _auditService.AppendAsync(
            "admin",
            "payout.statement.finalized",
            "PayoutStatement",
            statementId.ToString(),
            beforeJson: null,
            afterJson: $"{{\"netPayout\":{statement.NetPayout}}}",
            cancellationToken);

        return statement;
    }

    public async Task<PayoutStatement?> PushToErpAsync(int statementId, CancellationToken cancellationToken)
    {
        if (!await _licenceGate.AllowsMarketplaceAsync(cancellationToken))
            return null;

        var statement = await _repository.GetByIdAsync(statementId, cancellationToken);
        if (statement is null || statement.Status is not (PayoutStatementStatus.Finalized or PayoutStatementStatus.PushedToErp))
            return statement;

        var payload = JsonSerializer.Serialize(new
        {
            eventType = "payout.journal",
            statementId = statement.Id,
            vendorId = statement.VendorId,
            periodStartUtc = statement.PeriodStartUtc,
            periodEndUtc = statement.PeriodEndUtc,
            grossSales = statement.GrossSales,
            totalCommission = statement.TotalCommission,
            totalRefunds = statement.TotalRefunds,
            totalAdjustments = statement.TotalAdjustments,
            netPayout = statement.NetPayout
        });

        var jobId = await _erpSync.QueueSyncAsync(
            ErpSyncEntityType.PayoutJournal,
            ErpSyncDirection.PushToErp,
            $"payout:{statement.Id}",
            payload,
            cancellationToken);

        statement.Status = PayoutStatementStatus.PushedToErp;
        statement.ErpReferenceId = jobId.ToString();
        statement.ErpReportedTotal = statement.NetPayout;
        await _repository.UpdateAsync(statement, cancellationToken);

        await _auditService.AppendAsync(
            "admin",
            "payout.statement.pushed_erp",
            "PayoutStatement",
            statementId.ToString(),
            beforeJson: null,
            afterJson: $"{{\"erpJobId\":\"{jobId}\"}}",
            cancellationToken);

        return statement;
    }

    public async Task<PayoutReconciliationResult?> ReconcileAsync(int statementId, CancellationToken cancellationToken)
    {
        if (!await _licenceGate.AllowsMarketplaceAsync(cancellationToken))
            return null;

        var statement = await _repository.GetByIdAsync(statementId, cancellationToken);
        if (statement is null || !statement.ErpReportedTotal.HasValue)
            return null;

        var result = _builder.Reconcile(statement.NetPayout, statement.ErpReportedTotal.Value);
        if (result.WithinTolerance)
        {
            statement.Status = PayoutStatementStatus.Reconciled;
            await _repository.UpdateAsync(statement, cancellationToken);

            await _auditService.AppendAsync(
                "admin",
                "payout.statement.reconciled",
                "PayoutStatement",
                statementId.ToString(),
                beforeJson: null,
                afterJson: $"{{\"variance\":{result.Variance}}}",
                cancellationToken);
        }

        return result;
    }

    public async Task<PayoutAdjustment?> AddAdjustmentAsync(
        int vendorId,
        decimal amount,
        string reasonCode,
        string? notes,
        string actor,
        CancellationToken cancellationToken)
    {
        if (!await _licenceGate.AllowsMarketplaceAsync(cancellationToken))
            return null;

        var adjustment = new PayoutAdjustment
        {
            VendorId = vendorId,
            Amount = amount,
            ReasonCode = reasonCode,
            Notes = notes,
            CreatedBy = actor,
            CreatedUtc = DateTime.UtcNow
        };

        await _repository.InsertAdjustmentAsync(adjustment, cancellationToken);

        await _auditService.AppendAsync(
            actor,
            "payout.adjustment.created",
            "PayoutAdjustment",
            adjustment.Id.ToString(),
            beforeJson: null,
            afterJson: $"{{\"vendorId\":{vendorId},\"amount\":{amount},\"reasonCode\":\"{reasonCode}\"}}",
            cancellationToken);

        return adjustment;
    }

    public Task<IReadOnlyList<PayoutStatement>> ListAsync(int vendorId, CancellationToken cancellationToken)
        => _repository.ListByVendorAsync(vendorId, cancellationToken);

    public Task<PayoutStatement?> GetAsync(int statementId, CancellationToken cancellationToken)
        => _repository.GetByIdAsync(statementId, cancellationToken);

    public Task<PayoutStatement?> GetLatestForVendorAsync(int vendorId, CancellationToken cancellationToken)
        => _repository.GetLatestByVendorAsync(vendorId, cancellationToken);
}
