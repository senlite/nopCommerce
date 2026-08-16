namespace TwinParticles.CheckEngine.Domain.Ai;

public sealed class AiUsageSummary
{
    public string FeatureKey { get; init; } = string.Empty;

    public int TodayTokens { get; init; }

    public int Last7DaysTokens { get; init; }

    public int Last30DaysTokens { get; init; }

    public int TodayAttempts { get; init; }

    public int TodayFailures { get; init; }

    public int Last7DaysAttempts { get; init; }

    public int Last7DaysFailures { get; init; }

    public int Last30DaysAttempts { get; init; }

    public int Last30DaysFailures { get; init; }

    public decimal TodayEstimatedCostUsd { get; init; }

    public decimal Last7DaysEstimatedCostUsd { get; init; }

    public decimal Last30DaysEstimatedCostUsd { get; init; }

    public static double FailureRate(int attempts, int failures) =>
        attempts <= 0 ? 0 : (double)failures / attempts;
}
