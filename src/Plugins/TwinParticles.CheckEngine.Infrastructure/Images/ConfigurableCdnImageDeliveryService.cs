using System.Threading;
using System.Threading.Tasks;
using TwinParticles.CheckEngine.Domain.Images;

namespace TwinParticles.CheckEngine.Infrastructure.Images;

public sealed class ConfigurableCdnImageDeliveryService : IImageDeliveryService
{
    public Task<string?> GetVariantUrlAsync(int pictureId, ImageVariant variant, CancellationToken cancellationToken)
    {
        return Task.FromResult<string?>($"/images/{pictureId}/{variant.ToString().ToLowerInvariant()}");
    }
}
