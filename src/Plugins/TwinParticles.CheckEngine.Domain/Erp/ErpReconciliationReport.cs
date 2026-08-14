using System;
using System.Collections.Generic;

namespace TwinParticles.CheckEngine.Domain.Erp;

public sealed class ErpReconciliationReport
{
    public DateTime GeneratedUtc { get; set; }

    public int TotalJobs { get; set; }

    public int SuccessfulJobs { get; set; }

    public int FailedJobs { get; set; }

    public IReadOnlyList<string> Issues { get; set; } = [];

    /// <summary>Per-metric cross-system totals comparison (order count, payment total, inventory units).</summary>
    public IReadOnlyList<ErpReconciliationVariance> Variances { get; set; } = [];

    /// <summary>True when any compared metric fell outside tolerance and both sides were available.</summary>
    public bool HasFinancialDiscrepancy { get; set; }

    /// <summary>The reconciliation window covered by <see cref="Variances"/>, when computed.</summary>
    public DateTime? WindowFromUtc { get; set; }

    public DateTime? WindowToUtc { get; set; }
}
