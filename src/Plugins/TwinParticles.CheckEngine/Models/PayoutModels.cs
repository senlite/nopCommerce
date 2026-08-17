namespace TwinParticles.CheckEngine.Models;

public sealed class PayoutGenerateModel
{
    public int VendorId { get; set; }

    public DateTime PeriodStartUtc { get; set; }

    public DateTime PeriodEndUtc { get; set; }
}

public sealed class PayoutAdjustmentModel
{
    public int VendorId { get; set; }

    public decimal Amount { get; set; }

    public string ReasonCode { get; set; } = string.Empty;

    public string? Notes { get; set; }
}

public sealed class PayoutStatementActionModel
{
    public int StatementId { get; set; }
}
