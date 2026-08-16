namespace TwinParticles.CheckEngine.Application.Ai;

public sealed class AiSpendGuardResult
{
    public bool Allowed { get; init; }

    public string? ErrorCode { get; init; }

    public bool BudgetAlertRequired { get; init; }

    public string FeatureKey { get; init; } = string.Empty;

    public int Usage { get; init; }

    public int Ceiling { get; init; }

    public static AiSpendGuardResult Permit(string featureKey) =>
        new() { Allowed = true, FeatureKey = featureKey };

    public static AiSpendGuardResult Deny(
        string featureKey,
        string errorCode,
        bool budgetAlertRequired = false,
        int usage = 0,
        int ceiling = 0) =>
        new()
        {
            Allowed = false,
            FeatureKey = featureKey,
            ErrorCode = errorCode,
            BudgetAlertRequired = budgetAlertRequired,
            Usage = usage,
            Ceiling = ceiling
        };
}
