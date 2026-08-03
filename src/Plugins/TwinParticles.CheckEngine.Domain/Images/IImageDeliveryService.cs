using System.Threading;
using System.Threading.Tasks;

namespace TwinParticles.CheckEngine.Domain.Images;

public interface IImageDeliveryService
{
    Task<string?> GetVariantUrlAsync(int pictureId, ImageVariant variant, CancellationToken cancellationToken);
}
