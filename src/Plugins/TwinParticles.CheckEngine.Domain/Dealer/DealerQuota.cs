namespace TwinParticles.CheckEngine.Domain.Dealer;

public sealed class DealerQuota
{
    public int Id { get; set; }

    public int DealerAccountId { get; set; }

    public decimal SpendCeiling { get; set; }

    public decimal SpendUsed { get; set; }
}
