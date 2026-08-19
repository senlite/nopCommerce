namespace TwinParticles.CheckEngine.Domain.Dealer;

public sealed class DealerAccount
{
    public int Id { get; set; }

    public int CustomerId { get; set; }

    public string DisplayName { get; set; } = string.Empty;

    public int? DefaultPriceListId { get; set; }

    public bool IsActive { get; set; } = true;
}
