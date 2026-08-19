namespace TwinParticles.CheckEngine.Domain.Fleet;

public sealed class FleetApprovalRequest
{
    public int Id { get; set; }

    public int FleetAccountId { get; set; }

    public int FleetVehicleId { get; set; }

    public int ProductId { get; set; }

    public int Quantity { get; set; }

    public int BudgetCentreId { get; set; }

    public int RequesterCustomerId { get; set; }

    public FleetApprovalStatus Status { get; set; }

    public string? RejectionReason { get; set; }

    public int? OrderId { get; set; }

    public DateTimeOffset CreatedUtc { get; set; }

    public DateTimeOffset UpdatedUtc { get; set; }
}
