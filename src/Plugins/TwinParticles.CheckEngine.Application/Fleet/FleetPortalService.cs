using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TwinParticles.CheckEngine.Application.Fitment;
using TwinParticles.CheckEngine.Application.Licensing;
using TwinParticles.CheckEngine.Application.Vehicle.Vin;
using TwinParticles.CheckEngine.Domain.Fitment;
using TwinParticles.CheckEngine.Application.Portals;
using TwinParticles.CheckEngine.Domain.Fleet;
using TwinParticles.CheckEngine.Domain.Performance;
using TwinParticles.CheckEngine.Domain.Portals;
using TwinParticles.CheckEngine.Domain.Security;

namespace TwinParticles.CheckEngine.Application.Fleet;

public sealed class FleetPortalService
{
    private readonly IFleetPortalRepository _repository;
    private readonly VinDecodeApplicationService _vinDecode;
    private readonly FitmentEvaluationService _fitment;
    private readonly IPortalOrderBridge _orderBridge;
    private readonly FleetPortalLicenceGate _licenceGate;
    private readonly ICheckEngineAuditService _auditService;
    private readonly ICheckEngineClock _clock;

    public FleetPortalService(
        IFleetPortalRepository repository,
        VinDecodeApplicationService vinDecode,
        FitmentEvaluationService fitment,
        IPortalOrderBridge orderBridge,
        FleetPortalLicenceGate licenceGate,
        ICheckEngineAuditService auditService,
        ICheckEngineClock clock)
    {
        _repository = repository;
        _vinDecode = vinDecode;
        _fitment = fitment;
        _orderBridge = orderBridge;
        _licenceGate = licenceGate;
        _auditService = auditService;
        _clock = clock;
    }

    public async Task<FleetImportResult> ImportFleetVinsAsync(int fleetAccountId, IReadOnlyList<string> vins, CancellationToken cancellationToken)
    {
        if (!await _licenceGate.AllowsFleetAsync(cancellationToken))
            return FleetImportResult.Fail(FleetErrorCodes.LicenceDenied);

        var account = await _repository.GetAccountByIdAsync(fleetAccountId, cancellationToken);
        if (account is null || !account.IsActive)
            return FleetImportResult.Fail(FleetErrorCodes.NotFound);

        var rows = new List<FleetVinImportRow>();
        foreach (var rawVin in vins.Where(v => !string.IsNullOrWhiteSpace(v)))
        {
            var decode = await _vinDecode.DecodeAsync(rawVin.Trim(), cancellationToken);
            var configurationId = decode.Outcome == "SingleMatch"
                ? decode.Candidates.FirstOrDefault()?.VehicleConfigurationId
                : null;

            if (configurationId.HasValue)
            {
                var vehicleId = await _repository.InsertVehicleAsync(new FleetVehicle
                {
                    FleetAccountId = fleetAccountId,
                    VehicleConfigurationId = configurationId,
                    Vin = decode.NormalizedVin
                }, cancellationToken);

                rows.Add(new FleetVinImportRow
                {
                    Vin = decode.NormalizedVin ?? rawVin.Trim(),
                    Outcome = "Registered",
                    VehicleConfigurationId = configurationId,
                    ReasonCode = $"vehicleId:{vehicleId}"
                });
            }
            else
            {
                rows.Add(new FleetVinImportRow
                {
                    Vin = rawVin.Trim(),
                    Outcome = "Failed",
                    ReasonCode = decode.ReasonCode ?? decode.Outcome
                });
            }
        }

        var batch = new FleetVinImportBatch
        {
            FleetAccountId = fleetAccountId,
            TotalRows = rows.Count,
            SucceededRows = rows.Count(row => row.Outcome == "Registered"),
            CreatedUtc = _clock.UtcNow,
            Rows = rows
        };

        batch.Id = await _repository.InsertImportBatchAsync(batch, cancellationToken);
        await AuditAsync("fleet.vin.import", batch.Id, cancellationToken);
        return FleetImportResult.Ok(batch);
    }

    public async Task<FleetApprovalResult> SubmitApprovalRequestAsync(SubmitApprovalRequest request, CancellationToken cancellationToken)
    {
        if (!await _licenceGate.AllowsFleetAsync(cancellationToken))
            return FleetApprovalResult.Fail(FleetErrorCodes.LicenceDenied);

        var vehicle = await _repository.GetVehicleAsync(request.FleetVehicleId, cancellationToken);
        if (vehicle is null || vehicle.FleetAccountId != request.FleetAccountId || !vehicle.VehicleConfigurationId.HasValue)
            return FleetApprovalResult.Fail(FleetErrorCodes.NotFound);

        var evaluation = await _fitment.EvaluateAsync(new FitmentEvaluationContext
        {
            ProductId = request.ProductId,
            VehicleConfigurationId = vehicle.VehicleConfigurationId.Value
        }, cancellationToken);

        if (evaluation.Outcome == FitmentStatus.DoesNotFit)
            return FleetApprovalResult.Fail(FleetErrorCodes.FitmentBlocked);

        var now = _clock.UtcNow;
        var approval = new FleetApprovalRequest
        {
            FleetAccountId = request.FleetAccountId,
            FleetVehicleId = request.FleetVehicleId,
            ProductId = request.ProductId,
            Quantity = request.Quantity,
            BudgetCentreId = request.BudgetCentreId,
            RequesterCustomerId = request.RequesterCustomerId,
            Status = FleetApprovalStatus.Pending,
            CreatedUtc = now,
            UpdatedUtc = now
        };

        approval.Id = await _repository.InsertApprovalRequestAsync(approval, cancellationToken);
        await AuditAsync("fleet.approval.submit", approval.Id, cancellationToken);
        return FleetApprovalResult.Ok(approval);
    }

    public async Task<FleetApprovalResult> DecideApprovalRequestAsync(int requestId, bool approve, string? rejectionReason, CancellationToken cancellationToken)
    {
        if (!await _licenceGate.AllowsFleetAsync(cancellationToken))
            return FleetApprovalResult.Fail(FleetErrorCodes.LicenceDenied);

        var approval = await _repository.GetApprovalRequestAsync(requestId, cancellationToken);
        if (approval is null || approval.Status != FleetApprovalStatus.Pending)
            return FleetApprovalResult.Fail(FleetErrorCodes.InvalidDecision);

        if (!approve)
        {
            approval.Status = FleetApprovalStatus.Rejected;
            approval.RejectionReason = rejectionReason;
            approval.UpdatedUtc = _clock.UtcNow;
            await _repository.UpdateApprovalRequestAsync(approval, cancellationToken);
            await AuditAsync("fleet.approval.reject", approval.Id, cancellationToken);
            return FleetApprovalResult.Ok(approval);
        }

        var centre = await _repository.GetBudgetCentreAsync(approval.BudgetCentreId, cancellationToken);
        if (centre is null)
            return FleetApprovalResult.Fail(FleetErrorCodes.NotFound);

        var lineCost = approval.Quantity * 1m;
        if (centre.SpendUsed + lineCost > centre.SpendLimit)
            return FleetApprovalResult.Fail(FleetErrorCodes.BudgetExceeded);

        var account = await _repository.GetAccountByIdAsync(approval.FleetAccountId, cancellationToken);
        if (account is null)
            return FleetApprovalResult.Fail(FleetErrorCodes.NotFound);

        var orderId = await _orderBridge.CreateTradeOrderAsync(account.CustomerId, new[]
        {
            new PortalOrderLine { ProductId = approval.ProductId, Quantity = approval.Quantity, UnitPrice = 1m }
        }, cancellationToken);

        centre.SpendUsed += lineCost;
        await _repository.UpdateBudgetCentreAsync(centre, cancellationToken);

        approval.Status = FleetApprovalStatus.Placed;
        approval.OrderId = orderId;
        approval.UpdatedUtc = _clock.UtcNow;
        await _repository.UpdateApprovalRequestAsync(approval, cancellationToken);
        await AuditAsync("fleet.approval.approve", approval.Id, cancellationToken);
        return FleetApprovalResult.Ok(approval);
    }

    public async Task<FleetPortalSnapshot?> GetDashboardAsync(int customerId, CancellationToken cancellationToken)
    {
        if (!await _licenceGate.AllowsFleetAsync(cancellationToken))
            return null;

        var account = await _repository.GetAccountByCustomerIdAsync(customerId, cancellationToken);
        if (account is null || !account.IsActive)
            return null;

        var vehicles = await _repository.GetVehiclesAsync(account.Id, cancellationToken);
        var centres = await _repository.ListBudgetCentresByAccountAsync(account.Id, cancellationToken);
        var approvals = await _repository.ListApprovalRequestsByAccountAsync(account.Id, cancellationToken);
        return new FleetPortalSnapshot
        {
            Account = account,
            Vehicles = vehicles,
            BudgetCentres = centres,
            ApprovalRequests = approvals
        };
    }

    private Task AuditAsync(string action, int entityId, CancellationToken cancellationToken)
        => _auditService.AppendAsync(
            "check-engine",
            action,
            "fleet",
            entityId.ToString(),
            beforeJson: null,
            afterJson: "{}",
            cancellationToken);
}

public sealed class SubmitApprovalRequest
{
    public int FleetAccountId { get; init; }

    public int FleetVehicleId { get; init; }

    public int ProductId { get; init; }

    public int Quantity { get; init; }

    public int BudgetCentreId { get; init; }

    public int RequesterCustomerId { get; init; }
}

public sealed class FleetImportResult
{
    public bool Success { get; init; }

    public string? ErrorCode { get; init; }

    public FleetVinImportBatch? Batch { get; init; }

    public static FleetImportResult Ok(FleetVinImportBatch batch) => new() { Success = true, Batch = batch };

    public static FleetImportResult Fail(string errorCode) => new() { Success = false, ErrorCode = errorCode };
}

public sealed class FleetApprovalResult
{
    public bool Success { get; init; }

    public string? ErrorCode { get; init; }

    public FleetApprovalRequest? Request { get; init; }

    public static FleetApprovalResult Ok(FleetApprovalRequest request) => new() { Success = true, Request = request };

    public static FleetApprovalResult Fail(string errorCode) => new() { Success = false, ErrorCode = errorCode };
}
