namespace TwinParticles.CheckEngine.Domain.Erp;

/// <summary>
/// Cross-system financial and inventory totals for a reconciliation window. The same shape is
/// produced for the local nopCommerce store and for the ERP system so the two can be compared.
/// </summary>
public sealed class ErpReconciliationTotals
{
    public int OrderCount { get; init; }

    public decimal PaymentTotal { get; init; }

    public int InventoryUnits { get; init; }

    /// <summary>False when the source could not be read (e.g. ERP unconfigured or unreachable).</summary>
    public bool IsAvailable { get; init; } = true;

    public static ErpReconciliationTotals Unavailable { get; } = new() { IsAvailable = false };
}
