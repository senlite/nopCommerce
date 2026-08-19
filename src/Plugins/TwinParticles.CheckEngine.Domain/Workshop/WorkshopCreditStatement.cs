using System;
using System.Collections.Generic;

namespace TwinParticles.CheckEngine.Domain.Workshop;

public sealed class WorkshopCreditStatement
{
    public int Id { get; set; }

    public int WorkshopAccountId { get; set; }

    public DateTime PeriodStartUtc { get; set; }

    public DateTime PeriodEndUtc { get; set; }

    public decimal OpeningBalance { get; set; }

    public decimal InvoicedTotal { get; set; }

    public decimal ClosingBalance { get; set; }

    public DateTime CreatedUtc { get; set; }

    public IReadOnlyList<WorkshopCreditStatementLine> Lines { get; set; } = [];
}

public sealed class WorkshopCreditStatementLine
{
    public int Id { get; set; }

    public int StatementId { get; set; }

    public int JobId { get; set; }

    public int? OrderId { get; set; }

    public decimal PartsTotal { get; set; }

    public decimal LabourTotal { get; set; }

    public decimal LineTotal { get; set; }

    public DateTime InvoicedUtc { get; set; }
}

public sealed class WorkshopInvoicedJobSummary
{
    public int JobId { get; set; }

    public int? OrderId { get; set; }

    public decimal LabourEstimate { get; set; }

    public decimal PartsTotal { get; set; }

    public DateTime InvoicedUtc { get; set; }
}
