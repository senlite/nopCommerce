using System;

namespace TwinParticles.CheckEngine.Domain.Marketplace;

public sealed class VendorDashboardSnapshot
{
    public int? VendorId { get; init; }

    public int ProductCount { get; init; }

    public int TotalStockUnits { get; init; }

    public int LowStockCount { get; init; }

    public int OrderCount { get; init; }

    public int CustomerCount { get; init; }

    public VendorFitmentProposalSummary FitmentProposals { get; init; } = new();

    public VendorScorecard Scorecard { get; init; } = VendorScorecard.Empty();

    public VendorStatementSummary Statements { get; init; } = new();
}

public sealed class VendorStatementSummary
{
    public bool Available { get; init; }

    public int? StatementId { get; init; }

    public decimal? NetPayout { get; init; }

    public PayoutStatementStatus? Status { get; init; }

    public DateTime? PeriodStartUtc { get; init; }

    public DateTime? PeriodEndUtc { get; init; }

    public string? MessageKey { get; init; }
}
