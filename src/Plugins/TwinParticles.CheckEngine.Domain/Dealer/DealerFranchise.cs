namespace TwinParticles.CheckEngine.Domain.Dealer;

public sealed class DealerFranchise
{
    public int Id { get; set; }

    public int DealerAccountId { get; set; }

    public int MakeId { get; set; }

    public string FranchiseLabel { get; set; } = string.Empty;
}
