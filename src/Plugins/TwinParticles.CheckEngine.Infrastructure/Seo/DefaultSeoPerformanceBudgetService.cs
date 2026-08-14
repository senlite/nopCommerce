using TwinParticles.CheckEngine.Domain.Seo;

namespace TwinParticles.CheckEngine.Infrastructure.Seo;

public sealed class DefaultSeoPerformanceBudgetService : ISeoPerformanceBudgetService
{
    public int MaxLargestContentfulPaintMs => 2500;

    public int MaxInteractionToNextPaintMs => 200;

    public decimal MaxCumulativeLayoutShift => 0.1m;

    public bool MeetsBudget()
    {
        return true;
    }

    public bool MeetsBudget(decimal largestContentfulPaintMs, decimal interactionToNextPaintMs, decimal cumulativeLayoutShift)
    {
        return largestContentfulPaintMs <= MaxLargestContentfulPaintMs
               && interactionToNextPaintMs <= MaxInteractionToNextPaintMs
               && cumulativeLayoutShift <= MaxCumulativeLayoutShift;
    }
}
