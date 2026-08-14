namespace TwinParticles.CheckEngine.Domain.Vehicle;

/// <summary>
/// Tunable VIN decode policy (doc 13). Below the auto-accept threshold the customer must
/// disambiguate even when only one candidate is returned.
/// </summary>
public sealed class VinDecodeOptions
{
    public const decimal DefaultAutoAcceptConfidenceThreshold = 0.85m;

    public decimal AutoAcceptConfidenceThreshold { get; init; } = DefaultAutoAcceptConfidenceThreshold;

    public static VinDecodeOptions Current { get; set; } = new();
}
