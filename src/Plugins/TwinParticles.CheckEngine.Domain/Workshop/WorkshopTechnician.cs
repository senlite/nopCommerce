namespace TwinParticles.CheckEngine.Domain.Workshop;

public sealed class WorkshopTechnician
{
    public int Id { get; set; }

    public int WorkshopAccountId { get; set; }

    public int CustomerId { get; set; }

    public bool CanRaiseInvoice { get; set; }

    public bool IsFrontDesk { get; set; }
}
