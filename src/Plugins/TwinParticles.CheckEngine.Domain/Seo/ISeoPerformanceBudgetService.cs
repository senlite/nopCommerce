namespace TwinParticles.CheckEngine.Domain.Seo;

public interface ISeoPerformanceBudgetService
{
    int MaxLargestContentfulPaintMs { get; }

    int MaxInteractionToNextPaintMs { get; }

    decimal MaxCumulativeLayoutShift { get; }

    bool MeetsBudget();

    bool MeetsBudget(decimal largestContentfulPaintMs, decimal interactionToNextPaintMs, decimal cumulativeLayoutShift);
}
