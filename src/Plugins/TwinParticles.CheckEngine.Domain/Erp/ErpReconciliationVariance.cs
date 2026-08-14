namespace TwinParticles.CheckEngine.Domain.Erp;

/// <summary>
/// A single reconciled metric comparing the local store to the ERP system for a window.
/// </summary>
public sealed class ErpReconciliationVariance
{
    public required string Metric { get; init; }

    public decimal Local { get; init; }

    public decimal Erp { get; init; }

    public decimal AbsoluteVariance { get; init; }

    public bool WithinTolerance { get; init; }
}
