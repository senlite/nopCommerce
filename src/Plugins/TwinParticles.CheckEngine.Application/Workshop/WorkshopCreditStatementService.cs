using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TwinParticles.CheckEngine.Application.Licensing;
using TwinParticles.CheckEngine.Domain.Performance;
using TwinParticles.CheckEngine.Domain.Security;
using TwinParticles.CheckEngine.Domain.Workshop;

namespace TwinParticles.CheckEngine.Application.Workshop;

public sealed class WorkshopCreditStatementService
{
    private readonly IWorkshopJobRepository _repository;
    private readonly WorkshopPortalLicenceGate _licenceGate;
    private readonly ICheckEngineAuditService _auditService;
    private readonly ICheckEngineClock _clock;

    public WorkshopCreditStatementService(
        IWorkshopJobRepository repository,
        WorkshopPortalLicenceGate licenceGate,
        ICheckEngineAuditService auditService,
        ICheckEngineClock clock)
    {
        _repository = repository;
        _licenceGate = licenceGate;
        _auditService = auditService;
        _clock = clock;
    }

    public async Task<IReadOnlyList<WorkshopCreditStatement>> ListStatementsAsync(
        int workshopAccountId,
        CancellationToken cancellationToken)
    {
        if (!await _licenceGate.AllowsWorkshopAsync(cancellationToken))
            return Array.Empty<WorkshopCreditStatement>();

        return await _repository.ListCreditStatementsAsync(workshopAccountId, cancellationToken);
    }

    public async Task<WorkshopCreditStatement?> GenerateStatementAsync(
        int workshopAccountId,
        DateTime periodStartUtc,
        DateTime periodEndUtc,
        CancellationToken cancellationToken)
    {
        if (!await _licenceGate.AllowsWorkshopAsync(cancellationToken))
            return null;

        if (periodEndUtc <= periodStartUtc)
            return null;

        var account = await _repository.GetAccountByIdAsync(workshopAccountId, cancellationToken);
        if (account is null || !account.IsActive)
            return null;

        var invoiced = await _repository.ListInvoicedJobsInPeriodAsync(
            workshopAccountId,
            periodStartUtc,
            periodEndUtc,
            cancellationToken);

        var invoicedTotal = invoiced.Sum(job => job.PartsTotal + job.LabourEstimate);
        var openingBalance = account.CreditUsed - invoicedTotal;
        if (openingBalance < 0m)
            openingBalance = 0m;

        var statement = new WorkshopCreditStatement
        {
            WorkshopAccountId = workshopAccountId,
            PeriodStartUtc = periodStartUtc,
            PeriodEndUtc = periodEndUtc,
            OpeningBalance = openingBalance,
            InvoicedTotal = invoicedTotal,
            ClosingBalance = account.CreditUsed,
            CreatedUtc = _clock.UtcNow.UtcDateTime
        };

        statement.Id = await _repository.InsertCreditStatementAsync(statement, cancellationToken);

        foreach (var job in invoiced)
        {
            await _repository.InsertCreditStatementLineAsync(new WorkshopCreditStatementLine
            {
                StatementId = statement.Id,
                JobId = job.JobId,
                OrderId = job.OrderId,
                PartsTotal = job.PartsTotal,
                LabourTotal = job.LabourEstimate,
                LineTotal = job.PartsTotal + job.LabourEstimate,
                InvoicedUtc = job.InvoicedUtc
            }, cancellationToken);
        }

        await _auditService.AppendAsync(
            "check-engine",
            "workshop.credit_statement.generate",
            "workshop",
            statement.Id.ToString(),
            beforeJson: null,
            afterJson: $"{{\"accountId\":{workshopAccountId},\"invoicedTotal\":{invoicedTotal}}}",
            cancellationToken);

        return await _repository.GetCreditStatementAsync(statement.Id, cancellationToken);
    }
}
