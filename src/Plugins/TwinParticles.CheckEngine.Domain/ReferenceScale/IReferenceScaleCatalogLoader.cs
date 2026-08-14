using System.Threading;
using System.Threading.Tasks;

namespace TwinParticles.CheckEngine.Domain.ReferenceScale;

public interface IReferenceScaleCatalogLoader
{
    Task<ReferenceScaleStatus> GetStatusAsync(CancellationToken cancellationToken);

    Task<ReferenceScaleLoadResult> LoadAsync(ReferenceScaleLoadRequest request, CancellationToken cancellationToken);

    Task PurgeAsync(CancellationToken cancellationToken);
}
