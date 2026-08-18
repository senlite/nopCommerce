namespace TwinParticles.CheckEngine.Domain.Workshop;

public sealed class WorkshopJob
{
    public int Id { get; set; }

    public int WorkshopAccountId { get; set; }

    public int? WorkshopCustomerId { get; set; }

    public int? AssignedTechnicianCustomerId { get; set; }

    public WorkshopJobStatus Status { get; set; }

    public decimal LabourEstimate { get; set; }

    public int? OrderId { get; set; }

    public DateTimeOffset CreatedUtc { get; set; }

    public DateTimeOffset UpdatedUtc { get; set; }
}
