using TwinParticles.CheckEngine.Domain.Erp;

namespace TwinParticles.CheckEngine.Infrastructure.Erp;

public sealed class DefaultErpConflictResolutionService : IErpConflictResolutionService
{
    public bool CanAutoResolve(ErpSyncJob job)
    {
        return job.AttemptCount < 2;
    }

    public void ApplyAutoResolution(ErpSyncJob job)
    {
        if (job.Payload.Contains("force-fail"))
            job.Payload = job.Payload.Replace("force-fail", "retry-safe");

        job.ConflictCode = "erp.auto_retry";
    }
}
