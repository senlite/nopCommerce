namespace TwinParticles.CheckEngine.Domain.Workshop;

public sealed class WorkshopAccount
{
    public int Id { get; set; }

    public int CustomerId { get; set; }

    public string DisplayName { get; set; } = string.Empty;

    public decimal CreditLimit { get; set; }

    public decimal CreditUsed { get; set; }

    public int? DefaultPriceListId { get; set; }

    public bool IsActive { get; set; } = true;
}
