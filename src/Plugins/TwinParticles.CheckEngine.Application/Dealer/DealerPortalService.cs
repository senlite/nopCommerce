using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TwinParticles.CheckEngine.Application.Licensing;
using TwinParticles.CheckEngine.Domain.Dealer;
using TwinParticles.CheckEngine.Domain.Performance;
using TwinParticles.CheckEngine.Domain.Portals;
using TwinParticles.CheckEngine.Domain.Security;

namespace TwinParticles.CheckEngine.Application.Dealer;

public sealed class DealerPortalService
{
    private static readonly IReadOnlyDictionary<WarrantyClaimStatus, IReadOnlyCollection<WarrantyClaimStatus>> ClaimTransitions =
        new Dictionary<WarrantyClaimStatus, IReadOnlyCollection<WarrantyClaimStatus>>
        {
            [WarrantyClaimStatus.Submitted] = new[] { WarrantyClaimStatus.UnderReview, WarrantyClaimStatus.Rejected },
            [WarrantyClaimStatus.UnderReview] = new[] { WarrantyClaimStatus.MoreEvidenceRequested, WarrantyClaimStatus.Approved, WarrantyClaimStatus.Rejected },
            [WarrantyClaimStatus.MoreEvidenceRequested] = new[] { WarrantyClaimStatus.UnderReview, WarrantyClaimStatus.Rejected },
            [WarrantyClaimStatus.Approved] = Array.Empty<WarrantyClaimStatus>(),
            [WarrantyClaimStatus.Rejected] = Array.Empty<WarrantyClaimStatus>()
        };

    private readonly IDealerPortalRepository _repository;
    private readonly IPortalOrderBridge _orderBridge;
    private readonly DealerPortalLicenceGate _licenceGate;
    private readonly ICheckEngineAuditService _auditService;
    private readonly ICheckEngineClock _clock;

    public DealerPortalService(
        IDealerPortalRepository repository,
        IPortalOrderBridge orderBridge,
        DealerPortalLicenceGate licenceGate,
        ICheckEngineAuditService auditService,
        ICheckEngineClock clock)
    {
        _repository = repository;
        _orderBridge = orderBridge;
        _licenceGate = licenceGate;
        _auditService = auditService;
        _clock = clock;
    }

    public async Task<DealerCatalogResult> GetDealerCatalogViewAsync(int dealerAccountId, CancellationToken cancellationToken)
    {
        if (!await _licenceGate.AllowsDealerAsync(cancellationToken))
            return DealerCatalogResult.Fail(DealerErrorCodes.LicenceDenied);

        var account = await _repository.GetAccountByIdAsync(dealerAccountId, cancellationToken);
        if (account is null || !account.IsActive)
            return DealerCatalogResult.Fail(DealerErrorCodes.NotFound);

        var items = await _repository.GetCatalogViewAsync(dealerAccountId, cancellationToken);
        return DealerCatalogResult.Ok(items);
    }

    public async Task<DealerOrderResult> PlaceDealerOrderAsync(PlaceDealerOrderRequest request, CancellationToken cancellationToken)
    {
        if (!await _licenceGate.AllowsDealerAsync(cancellationToken))
            return DealerOrderResult.Fail(DealerErrorCodes.LicenceDenied);

        if (request.Lines.Count == 0)
            return DealerOrderResult.Fail(DealerErrorCodes.EmptyOrder);

        var account = await _repository.GetAccountByIdAsync(request.DealerAccountId, cancellationToken);
        if (account is null || !account.IsActive)
            return DealerOrderResult.Fail(DealerErrorCodes.NotFound);

        var quota = await _repository.GetQuotaAsync(request.DealerAccountId, cancellationToken);
        var orderTotal = request.Lines.Sum(line => line.UnitPrice * line.Quantity);

        foreach (var line in request.Lines)
        {
            var allocation = await _repository.GetAllocationForProductAsync(request.DealerAccountId, line.ProductId, cancellationToken);
            if (allocation is not null && allocation.PeriodUsedUnits + line.Quantity > allocation.PeriodCeilingUnits)
                return DealerOrderResult.Fail(DealerErrorCodes.AllocationExceeded);
        }

        if (quota is not null && quota.SpendUsed + orderTotal > quota.SpendCeiling)
            return DealerOrderResult.Fail(DealerErrorCodes.QuotaExceeded);

        var orderId = await _orderBridge.CreateTradeOrderAsync(account.CustomerId, request.Lines, cancellationToken);

        foreach (var line in request.Lines)
        {
            var allocation = await _repository.GetAllocationForProductAsync(request.DealerAccountId, line.ProductId, cancellationToken);
            if (allocation is null)
                continue;

            allocation.PeriodUsedUnits += line.Quantity;
            await _repository.UpdateAllocationAsync(allocation, cancellationToken);
        }

        if (quota is not null)
        {
            quota.SpendUsed += orderTotal;
            await _repository.UpdateQuotaAsync(quota, cancellationToken);
        }

        await AuditAsync("dealer.order.place", orderId, cancellationToken);
        return DealerOrderResult.Ok(orderId);
    }

    public async Task<WarrantyClaimResult> SubmitWarrantyClaimAsync(SubmitWarrantyClaimRequest request, CancellationToken cancellationToken)
    {
        if (!await _licenceGate.AllowsDealerAsync(cancellationToken))
            return WarrantyClaimResult.Fail(DealerErrorCodes.LicenceDenied);

        var account = await _repository.GetAccountByIdAsync(request.DealerAccountId, cancellationToken);
        if (account is null)
            return WarrantyClaimResult.Fail(DealerErrorCodes.NotFound);

        var now = _clock.UtcNow;
        var claim = new WarrantyClaim
        {
            DealerAccountId = request.DealerAccountId,
            OrderId = request.OrderId,
            OemNumber = request.OemNumber.Trim(),
            VehicleConfigurationId = request.VehicleConfigurationId,
            Status = WarrantyClaimStatus.Submitted,
            EvidenceJson = request.EvidenceJson ?? "[]",
            CreatedUtc = now,
            UpdatedUtc = now
        };

        claim.Id = await _repository.InsertWarrantyClaimAsync(claim, cancellationToken);
        await AuditAsync("dealer.warranty.submit", claim.Id, cancellationToken);
        return WarrantyClaimResult.Ok(claim);
    }

    public async Task<WarrantyClaimResult> TransitionWarrantyClaimAsync(int claimId, WarrantyClaimStatus targetStatus, CancellationToken cancellationToken)
    {
        if (!await _licenceGate.AllowsDealerAsync(cancellationToken))
            return WarrantyClaimResult.Fail(DealerErrorCodes.LicenceDenied);

        var claim = await _repository.GetWarrantyClaimAsync(claimId, cancellationToken);
        if (claim is null)
            return WarrantyClaimResult.Fail(DealerErrorCodes.NotFound);

        if (!ClaimTransitions.TryGetValue(claim.Status, out var allowed) || !allowed.Contains(targetStatus))
            return WarrantyClaimResult.Fail(DealerErrorCodes.InvalidTransition);

        claim.Status = targetStatus;
        claim.UpdatedUtc = _clock.UtcNow;
        await _repository.UpdateWarrantyClaimAsync(claim, cancellationToken);
        await AuditAsync("dealer.warranty.transition", claim.Id, cancellationToken);
        return WarrantyClaimResult.Ok(claim);
    }

    private Task AuditAsync(string action, int entityId, CancellationToken cancellationToken)
        => _auditService.AppendAsync(
            "check-engine",
            action,
            "dealer",
            entityId.ToString(),
            beforeJson: null,
            afterJson: "{}",
            cancellationToken);
}

public sealed class PlaceDealerOrderRequest
{
    public int DealerAccountId { get; init; }

    public IReadOnlyList<PortalOrderLine> Lines { get; init; } = Array.Empty<PortalOrderLine>();
}

public sealed class SubmitWarrantyClaimRequest
{
    public int DealerAccountId { get; init; }

    public int? OrderId { get; init; }

    public string OemNumber { get; init; } = string.Empty;

    public int VehicleConfigurationId { get; init; }

    public string? EvidenceJson { get; init; }
}

public sealed class DealerCatalogResult
{
    public bool Success { get; init; }

    public string? ErrorCode { get; init; }

    public IReadOnlyList<DealerCatalogItem> Items { get; init; } = Array.Empty<DealerCatalogItem>();

    public static DealerCatalogResult Ok(IReadOnlyList<DealerCatalogItem> items) => new() { Success = true, Items = items };

    public static DealerCatalogResult Fail(string errorCode) => new() { Success = false, ErrorCode = errorCode };
}

public sealed class DealerOrderResult
{
    public bool Success { get; init; }

    public string? ErrorCode { get; init; }

    public int? OrderId { get; init; }

    public static DealerOrderResult Ok(int orderId) => new() { Success = true, OrderId = orderId };

    public static DealerOrderResult Fail(string errorCode) => new() { Success = false, ErrorCode = errorCode };
}

public sealed class WarrantyClaimResult
{
    public bool Success { get; init; }

    public string? ErrorCode { get; init; }

    public WarrantyClaim? Claim { get; init; }

    public static WarrantyClaimResult Ok(WarrantyClaim claim) => new() { Success = true, Claim = claim };

    public static WarrantyClaimResult Fail(string errorCode) => new() { Success = false, ErrorCode = errorCode };
}
