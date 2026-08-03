namespace TwinParticles.CheckEngine.Domain.Erp;

public interface IErpConflictResolutionService
{
    bool CanAutoResolve(ErpSyncJob job);

    void ApplyAutoResolution(ErpSyncJob job);
}
