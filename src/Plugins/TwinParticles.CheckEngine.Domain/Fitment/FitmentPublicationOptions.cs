namespace TwinParticles.CheckEngine.Domain.Fitment;

/// <summary>
/// Operator-tunable publication thresholds (FR-313).
///
/// Safety-critical parts have an absolute floor that configuration cannot lower
/// (FR-316, AC-028.1): raising the bar is always allowed, lowering it past the floor is not.
/// </summary>
public sealed class FitmentPublicationOptions
{
    /// <summary>
    /// Lowest confidence a safety-critical claim may ever be published at, regardless of
    /// configuration. There is deliberately no setting that can reduce this.
    /// </summary>
    public const decimal SafetyCriticalConfidenceFloor = 0.95m;

    public static FitmentPublicationOptions Current { get; set; } = new();

    /// <summary>Minimum confidence for publishing a standard claim.</summary>
    public decimal MinPublishConfidence { get; set; } = 0.85m;

    /// <summary>
    /// Minimum confidence for publishing a safety-critical claim. Values below
    /// <see cref="SafetyCriticalConfidenceFloor"/> are ignored in favour of the floor.
    /// </summary>
    public decimal SafetyCriticalMinPublishConfidence { get; set; } = SafetyCriticalConfidenceFloor;

    /// <summary>Resolves the effective threshold for a claim's safety class.</summary>
    public decimal ResolveThreshold(SafetyClass safetyClass)
    {
        if (safetyClass != SafetyClass.SafetyCritical)
            return MinPublishConfidence;

        return SafetyCriticalMinPublishConfidence > SafetyCriticalConfidenceFloor
            ? SafetyCriticalMinPublishConfidence
            : SafetyCriticalConfidenceFloor;
    }
}
