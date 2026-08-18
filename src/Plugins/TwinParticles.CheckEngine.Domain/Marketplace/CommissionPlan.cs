using System;
using System.Collections.Generic;

namespace TwinParticles.CheckEngine.Domain.Marketplace;

public sealed class CommissionPlan
{
    public int Id { get; set; }

    public int VendorId { get; set; }

    public string Name { get; set; } = "Default";

    public bool IsActive { get; set; } = true;

    public IReadOnlyList<CommissionRule> Rules { get; set; } = [];
}

public sealed class CommissionRule
{
    public int Id { get; set; }

    public int PlanId { get; set; }

    public CommissionModelKind ModelKind { get; set; }

    public CommissionBasis Basis { get; set; } = CommissionBasis.LineSubtotal;

    public int Priority { get; set; }

    public decimal? FlatAmount { get; set; }

    public decimal? PercentageRate { get; set; }

    public int? CategoryId { get; set; }

    public IReadOnlyList<CommissionTierBand> TierBands { get; set; } = [];

    public DateTime? EffectiveFromUtc { get; set; }

    public DateTime? EffectiveToUtc { get; set; }

    public bool IsActive { get; set; } = true;
}

public sealed class CommissionTierBand
{
    public int Id { get; set; }

    public int RuleId { get; set; }

    public decimal MinVolume { get; set; }

    public decimal? MaxVolume { get; set; }

    public decimal PercentageRate { get; set; }
}
