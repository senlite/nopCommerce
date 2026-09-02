namespace TwinParticles.CheckEngine.Domain.Workshop;

public sealed class WorkshopJobLine
{
    public int Id { get; set; }

    public int JobId { get; set; }

    public int JobVehicleId { get; set; }

    public int ProductId { get; set; }

    public int Quantity { get; set; }

    public string FitmentOutcome { get; set; } = string.Empty;

    public decimal UnitPrice { get; set; }

    public int? InvoicedOrderId { get; set; }
}
