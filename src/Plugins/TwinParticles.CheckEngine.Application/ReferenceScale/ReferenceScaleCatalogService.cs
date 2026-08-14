using System.Threading;
using System.Threading.Tasks;
using TwinParticles.CheckEngine.Domain.ReferenceScale;

namespace TwinParticles.CheckEngine.Application.ReferenceScale;

public sealed class ReferenceScaleCatalogService
{
    private readonly IReferenceScaleCatalogLoader _loader;

    public ReferenceScaleCatalogService(IReferenceScaleCatalogLoader loader)
    {
        _loader = loader;
    }

    public Task<ReferenceScaleStatus> GetStatusAsync(CancellationToken cancellationToken)
        => _loader.GetStatusAsync(cancellationToken);

    public Task<ReferenceScaleLoadResult> LoadAsync(ReferenceScaleLoadRequest request, CancellationToken cancellationToken)
        => _loader.LoadAsync(request, cancellationToken);

    public Task PurgeAsync(CancellationToken cancellationToken)
        => _loader.PurgeAsync(cancellationToken);
}
