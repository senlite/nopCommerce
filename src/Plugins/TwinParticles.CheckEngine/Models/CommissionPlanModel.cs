namespace TwinParticles.CheckEngine.Models;

public sealed class CommissionPlanModel
{
    public int Id { get; set; }

    public int VendorId { get; set; }

    public string Name { get; set; } = "Default";

    public bool IsActive { get; set; } = true;

    public List<CommissionRuleModel> Rules { get; set; } = [];
}

public sealed class CommissionRuleModel
{
    public int Id { get; set; }

    public int ModelKind { get; set; }

    public int Basis { get; set; }

    public int Priority { get; set; }

    public decimal? FlatAmount { get; set; }

    public decimal? PercentageRate { get; set; }

    public int? CategoryId { get; set; }

    public List<CommissionTierBandModel> TierBands { get; set; } = [];

    public DateTime? EffectiveFromUtc { get; set; }

    public DateTime? EffectiveToUtc { get; set; }

    public bool IsActive { get; set; } = true;
}

public sealed class CommissionTierBandModel
{
    public decimal MinVolume { get; set; }

    public decimal? MaxVolume { get; set; }

    public decimal PercentageRate { get; set; }
}
