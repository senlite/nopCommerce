namespace TwinParticles.CheckEngine.Domain.Workshop;

public sealed class WorkshopCustomer
{
    public int Id { get; set; }

    public int WorkshopAccountId { get; set; }

    public string DisplayName { get; set; } = string.Empty;

    public string? ContactEmail { get; set; }
}
