namespace TwinParticles.CheckEngine.Domain.Fitment;

public sealed class FitmentEvaluationResult
{
    public FitmentStatus Outcome { get; init; } = FitmentStatus.Unknown;

    public decimal EffectiveConfidence { get; init; }

    public string? ReasonCode { get; init; }
}
