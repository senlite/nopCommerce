namespace TwinParticles.CheckEngine.Domain.Workshop;

public enum WorkshopJobStatus
{
    Draft = 0,
    InProgress = 10,
    AwaitingParts = 20,
    ReadyToInvoice = 30,
    Invoiced = 40,
    Cancelled = 90
}
