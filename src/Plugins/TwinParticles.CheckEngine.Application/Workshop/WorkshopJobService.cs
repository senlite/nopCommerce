using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TwinParticles.CheckEngine.Application.Fitment;
using TwinParticles.CheckEngine.Application.Licensing;
using TwinParticles.CheckEngine.Domain.Fitment;
using TwinParticles.CheckEngine.Domain.Performance;
using TwinParticles.CheckEngine.Domain.Portals;
using TwinParticles.CheckEngine.Domain.Security;
using TwinParticles.CheckEngine.Application.Portals;
using TwinParticles.CheckEngine.Domain.Workshop;

namespace TwinParticles.CheckEngine.Application.Workshop;

public sealed class WorkshopJobService
{
    private static readonly IReadOnlyDictionary<WorkshopJobStatus, IReadOnlyCollection<WorkshopJobStatus>> AllowedTransitions =
        new Dictionary<WorkshopJobStatus, IReadOnlyCollection<WorkshopJobStatus>>
        {
            [WorkshopJobStatus.Draft] = new[] { WorkshopJobStatus.InProgress, WorkshopJobStatus.Cancelled },
            [WorkshopJobStatus.InProgress] = new[] { WorkshopJobStatus.AwaitingParts, WorkshopJobStatus.ReadyToInvoice, WorkshopJobStatus.Cancelled },
            [WorkshopJobStatus.AwaitingParts] = new[] { WorkshopJobStatus.InProgress, WorkshopJobStatus.ReadyToInvoice, WorkshopJobStatus.Cancelled },
            [WorkshopJobStatus.ReadyToInvoice] = new[] { WorkshopJobStatus.Invoiced, WorkshopJobStatus.Cancelled },
            [WorkshopJobStatus.Invoiced] = Array.Empty<WorkshopJobStatus>(),
            [WorkshopJobStatus.Cancelled] = Array.Empty<WorkshopJobStatus>()
        };

    private readonly IWorkshopJobRepository _repository;
    private readonly FitmentEvaluationService _fitment;
    private readonly IPortalOrderBridge _orderBridge;
    private readonly WorkshopPortalLicenceGate _licenceGate;
    private readonly ICheckEngineAuditService _auditService;
    private readonly ICheckEngineClock _clock;

    public WorkshopJobService(
        IWorkshopJobRepository repository,
        FitmentEvaluationService fitment,
        IPortalOrderBridge orderBridge,
        WorkshopPortalLicenceGate licenceGate,
        ICheckEngineAuditService auditService,
        ICheckEngineClock clock)
    {
        _repository = repository;
        _fitment = fitment;
        _orderBridge = orderBridge;
        _licenceGate = licenceGate;
        _auditService = auditService;
        _clock = clock;
    }

    public async Task<WorkshopJobResult> CreateJobAsync(CreateJobRequest request, CancellationToken cancellationToken)
    {
        if (!await _licenceGate.AllowsWorkshopAsync(cancellationToken))
            return WorkshopJobResult.Fail(WorkshopErrorCodes.LicenceDenied);

        var account = await _repository.GetAccountByIdAsync(request.WorkshopAccountId, cancellationToken);
        if (account is null || !account.IsActive)
            return WorkshopJobResult.Fail(WorkshopErrorCodes.NotFound);

        var now = _clock.UtcNow;
        var job = new WorkshopJob
        {
            WorkshopAccountId = request.WorkshopAccountId,
            WorkshopCustomerId = request.WorkshopCustomerId,
            AssignedTechnicianCustomerId = request.TechnicianCustomerId,
            Status = WorkshopJobStatus.Draft,
            LabourEstimate = request.LabourEstimate,
            CreatedUtc = now,
            UpdatedUtc = now
        };

        job.Id = await _repository.InsertJobAsync(job, cancellationToken);

        foreach (var vehicle in request.Vehicles)
        {
            await _repository.InsertJobVehicleAsync(new WorkshopJobVehicle
            {
                JobId = job.Id,
                VehicleConfigurationId = vehicle.VehicleConfigurationId,
                Vin = vehicle.Vin,
                Label = vehicle.Label
            }, cancellationToken);
        }

        await AuditAsync("workshop.job.create", job.Id, cancellationToken);
        return WorkshopJobResult.Ok(job);
    }

    public async Task<WorkshopAllocateResult> AllocateJobLineAsync(AllocateJobLineRequest request, CancellationToken cancellationToken)
    {
        if (!await _licenceGate.AllowsWorkshopAsync(cancellationToken))
            return WorkshopAllocateResult.Fail(WorkshopErrorCodes.LicenceDenied);

        var job = await _repository.GetJobAsync(request.JobId, cancellationToken);
        if (job is null)
            return WorkshopAllocateResult.Fail(WorkshopErrorCodes.NotFound);

        if (job.Status is WorkshopJobStatus.Invoiced or WorkshopJobStatus.Cancelled)
            return WorkshopAllocateResult.Fail(WorkshopErrorCodes.InvalidTransition);

        var vehicle = await _repository.GetJobVehicleAsync(request.JobVehicleId, cancellationToken);
        if (vehicle is null || vehicle.JobId != job.Id)
            return WorkshopAllocateResult.Fail(WorkshopErrorCodes.NotFound);

        var evaluation = await _fitment.EvaluateAsync(new FitmentEvaluationContext
        {
            ProductId = request.ProductId,
            VehicleConfigurationId = vehicle.VehicleConfigurationId
        }, cancellationToken);

        if (evaluation.Outcome == FitmentStatus.DoesNotFit)
            return WorkshopAllocateResult.Fail(WorkshopErrorCodes.FitmentBlocked, evaluation);

        var account = await _repository.GetAccountByIdAsync(job.WorkshopAccountId, cancellationToken);
        var unitPrice = account?.DefaultPriceListId is int priceListId
            ? await _repository.ResolveTradePriceAsync(priceListId, request.ProductId, cancellationToken)
            : 0m;

        var line = new WorkshopJobLine
        {
            JobId = job.Id,
            JobVehicleId = vehicle.Id,
            ProductId = request.ProductId,
            Quantity = request.Quantity,
            FitmentOutcome = evaluation.Outcome.ToString(),
            UnitPrice = unitPrice
        };

        line.Id = await _repository.InsertJobLineAsync(line, cancellationToken);
        await AuditAsync("workshop.job.allocate", job.Id, cancellationToken);
        return WorkshopAllocateResult.Ok(line, evaluation);
    }

    public async Task<WorkshopJobResult> TransitionJobStatusAsync(int jobId, WorkshopJobStatus targetStatus, CancellationToken cancellationToken)
    {
        if (!await _licenceGate.AllowsWorkshopAsync(cancellationToken))
            return WorkshopJobResult.Fail(WorkshopErrorCodes.LicenceDenied);

        var job = await _repository.GetJobAsync(jobId, cancellationToken);
        if (job is null)
            return WorkshopJobResult.Fail(WorkshopErrorCodes.NotFound);

        if (!AllowedTransitions.TryGetValue(job.Status, out var allowed) || !allowed.Contains(targetStatus))
            return WorkshopJobResult.Fail(WorkshopErrorCodes.InvalidTransition);

        job.Status = targetStatus;
        job.UpdatedUtc = _clock.UtcNow;
        await _repository.UpdateJobAsync(job, cancellationToken);
        await AuditAsync("workshop.job.transition", job.Id, cancellationToken);
        return WorkshopJobResult.Ok(job);
    }

    public async Task<WorkshopInvoiceResult> RaiseJobInvoiceAsync(int jobId, CancellationToken cancellationToken)
    {
        if (!await _licenceGate.AllowsWorkshopAsync(cancellationToken))
            return WorkshopInvoiceResult.Fail(WorkshopErrorCodes.LicenceDenied);

        var job = await _repository.GetJobAsync(jobId, cancellationToken);
        if (job is null)
            return WorkshopInvoiceResult.Fail(WorkshopErrorCodes.NotFound);

        if (job.Status == WorkshopJobStatus.Invoiced)
            return WorkshopInvoiceResult.Fail(WorkshopErrorCodes.AlreadyInvoiced);

        if (job.Status != WorkshopJobStatus.ReadyToInvoice)
            return WorkshopInvoiceResult.Fail(WorkshopErrorCodes.InvalidTransition);

        var lines = await _repository.GetJobLinesAsync(jobId, cancellationToken);
        if (lines.Count == 0)
            return WorkshopInvoiceResult.Fail(WorkshopErrorCodes.EmptyJob);

        var account = await _repository.GetAccountByIdAsync(job.WorkshopAccountId, cancellationToken);
        if (account is null)
            return WorkshopInvoiceResult.Fail(WorkshopErrorCodes.NotFound);

        var partsTotal = lines.Sum(line => line.UnitPrice * line.Quantity);
        var invoiceTotal = partsTotal + job.LabourEstimate;
        if (account.CreditUsed + invoiceTotal > account.CreditLimit)
            return WorkshopInvoiceResult.Fail(WorkshopErrorCodes.CreditExceeded);

        var orderLines = lines
            .Select(line => new PortalOrderLine
            {
                ProductId = line.ProductId,
                Quantity = line.Quantity,
                UnitPrice = line.UnitPrice
            })
            .ToList();

        var orderId = await _orderBridge.CreateTradeOrderAsync(account.CustomerId, orderLines, cancellationToken);

        job.OrderId = orderId;
        job.Status = WorkshopJobStatus.Invoiced;
        job.UpdatedUtc = _clock.UtcNow;
        await _repository.UpdateJobAsync(job, cancellationToken);
        await AuditAsync("workshop.job.invoice", job.Id, cancellationToken);
        return WorkshopInvoiceResult.Ok(job, orderId);
    }

    public async Task<WorkshopPortalSnapshot?> GetDashboardAsync(int customerId, CancellationToken cancellationToken)
    {
        if (!await _licenceGate.AllowsWorkshopAsync(cancellationToken))
            return null;

        var account = await _repository.GetAccountByCustomerIdAsync(customerId, cancellationToken);
        if (account is null || !account.IsActive)
            return null;

        var jobs = await _repository.ListJobsByAccountAsync(account.Id, cancellationToken);
        return new WorkshopPortalSnapshot { Account = account, Jobs = jobs };
    }

    public async Task<WorkshopJobDetail?> GetJobDetailAsync(int jobId, CancellationToken cancellationToken)
    {
        if (!await _licenceGate.AllowsWorkshopAsync(cancellationToken))
            return null;

        var job = await _repository.GetJobAsync(jobId, cancellationToken);
        if (job is null)
            return null;

        var vehicles = await _repository.GetJobVehiclesAsync(jobId, cancellationToken);
        var lines = await _repository.GetJobLinesAsync(jobId, cancellationToken);
        return new WorkshopJobDetail { Job = job, Vehicles = vehicles, Lines = lines };
    }

    private Task AuditAsync(string action, int entityId, CancellationToken cancellationToken)
        => _auditService.AppendAsync(
            "check-engine",
            action,
            "workshop",
            entityId.ToString(),
            beforeJson: null,
            afterJson: "{}",
            cancellationToken);
}

public sealed class CreateJobRequest
{
    public int WorkshopAccountId { get; init; }

    public int? WorkshopCustomerId { get; init; }

    public int? TechnicianCustomerId { get; init; }

    public decimal LabourEstimate { get; init; }

    public IReadOnlyList<CreateJobVehicleRequest> Vehicles { get; init; } = Array.Empty<CreateJobVehicleRequest>();
}

public sealed class CreateJobVehicleRequest
{
    public int VehicleConfigurationId { get; init; }

    public string? Vin { get; init; }

    public string? Label { get; init; }
}

public sealed class AllocateJobLineRequest
{
    public int JobId { get; init; }

    public int JobVehicleId { get; init; }

    public int ProductId { get; init; }

    public int Quantity { get; init; }
}

public sealed class WorkshopJobResult
{
    public bool Success { get; init; }

    public string? ErrorCode { get; init; }

    public WorkshopJob? Job { get; init; }

    public static WorkshopJobResult Ok(WorkshopJob job) => new() { Success = true, Job = job };

    public static WorkshopJobResult Fail(string errorCode) => new() { Success = false, ErrorCode = errorCode };
}

public sealed class WorkshopAllocateResult
{
    public bool Success { get; init; }

    public string? ErrorCode { get; init; }

    public WorkshopJobLine? Line { get; init; }

    public FitmentEvaluationResult? Fitment { get; init; }

    public static WorkshopAllocateResult Ok(WorkshopJobLine line, FitmentEvaluationResult fitment)
        => new() { Success = true, Line = line, Fitment = fitment };

    public static WorkshopAllocateResult Fail(string errorCode, FitmentEvaluationResult? fitment = null)
        => new() { Success = false, ErrorCode = errorCode, Fitment = fitment };
}

public sealed class WorkshopInvoiceResult
{
    public bool Success { get; init; }

    public string? ErrorCode { get; init; }

    public WorkshopJob? Job { get; init; }

    public int? OrderId { get; init; }

    public static WorkshopInvoiceResult Ok(WorkshopJob job, int orderId)
        => new() { Success = true, Job = job, OrderId = orderId };

    public static WorkshopInvoiceResult Fail(string errorCode)
        => new() { Success = false, ErrorCode = errorCode };
}
