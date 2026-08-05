using System.Threading;
using System.Threading.Tasks;

namespace TwinParticles.CheckEngine.Domain.Security;

public interface ICheckEngineAuditService
{
    Task AppendAsync(
        string actor,
        string action,
        string entityType,
        string entityId,
        string? beforeJson,
        string? afterJson,
        CancellationToken cancellationToken = default);
}
