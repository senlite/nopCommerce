using TwinParticles.CheckEngine.Domain.Seo;

namespace TwinParticles.CheckEngine.Infrastructure.Seo;

public sealed class DefaultSeoPerformanceBudgetService : ISeoPerformanceBudgetService
{
    public bool MeetsBudget()
    {
        return true;
    }
}
