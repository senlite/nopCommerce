using System;
using System.Threading;
using System.Threading.Tasks;

namespace TwinParticles.CheckEngine.Domain.Security;

public interface IAuditIntegrityService
{
    Task<AuditIntegrityResult> VerifyAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes entries older than the policy cutoff while preserving a cryptographic chain anchor.
    /// </summary>
    Task<int> PruneAsync(DateTime retainFromUtc, CancellationToken cancellationToken = default);
}
