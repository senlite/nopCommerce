namespace TwinParticles.CheckEngine.Domain.Dealer;

public sealed class DealerAllocation
{
    public int Id { get; set; }

    public int DealerAccountId { get; set; }

    public int? ProductId { get; set; }

    public int? CategoryId { get; set; }

    public int PeriodCeilingUnits { get; set; }

    public int PeriodUsedUnits { get; set; }
}
