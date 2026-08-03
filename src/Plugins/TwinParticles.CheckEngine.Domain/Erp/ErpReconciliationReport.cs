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
}
