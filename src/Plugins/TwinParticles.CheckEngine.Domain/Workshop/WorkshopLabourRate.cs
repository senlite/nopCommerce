namespace TwinParticles.CheckEngine.Domain.Workshop;

public sealed class WorkshopLabourRate
{
    public int Id { get; set; }

    public int WorkshopAccountId { get; set; }

    public string OperationCode { get; set; } = string.Empty;

    public decimal HourlyRate { get; set; }
}
