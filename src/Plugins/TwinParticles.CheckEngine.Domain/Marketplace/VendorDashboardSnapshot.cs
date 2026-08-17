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

    public VendorStatementPlaceholder Statements { get; init; } = new();
}

public sealed class VendorStatementPlaceholder
{
    public bool Available { get; init; }

    public string MessageKey { get; init; } = "Plugins.TwinParticles.CheckEngine.Marketplace.Statements.Pending";
}
