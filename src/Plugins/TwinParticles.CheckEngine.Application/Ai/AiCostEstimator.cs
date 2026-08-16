using System;

namespace TwinParticles.CheckEngine.Application.Ai;

public static class AiCostEstimator
{
    public static decimal EstimateUsd(int tokenUsage, decimal costPer1KTokensUsd)
    {
        if (tokenUsage <= 0 || costPer1KTokensUsd <= 0m)
            return 0m;

        return Math.Round(tokenUsage / 1000m * costPer1KTokensUsd, 4, MidpointRounding.AwayFromZero);
    }
}
