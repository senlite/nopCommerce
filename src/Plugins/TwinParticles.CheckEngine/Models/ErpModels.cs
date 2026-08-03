using TwinParticles.CheckEngine.Domain.Erp;

namespace TwinParticles.CheckEngine.Models;

public sealed class ErpQueueRequestModel
{
    public ErpSyncEntityType EntityType { get; set; } = ErpSyncEntityType.Product;

    public ErpSyncDirection Direction { get; set; } = ErpSyncDirection.Bidirectional;

    public string LocalId { get; set; } = string.Empty;

    public string Payload { get; set; } = string.Empty;
}
