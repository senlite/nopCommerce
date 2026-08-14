using System.Threading;
using System.Threading.Tasks;
using Nop.Services.Media;
using TwinParticles.CheckEngine.Domain.Images;

namespace TwinParticles.CheckEngine.Infrastructure.Images;

public sealed class ConfigurableCdnImageDeliveryService : IImageDeliveryService
{
    public const int ListingSize = 320;
    public const int ProductSize = 800;
    public const int ZoomSize = 1600;

    private readonly IPictureService _pictureService;

    public ConfigurableCdnImageDeliveryService(IPictureService pictureService)
    {
        _pictureService = pictureService;
    }

    public async Task<string?> GetVariantUrlAsync(int pictureId, ImageVariant variant, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (pictureId <= 0)
            return null;

        var targetSize = variant switch
        {
            ImageVariant.Listing => ListingSize,
            ImageVariant.Product => ProductSize,
            ImageVariant.Zoom => ZoomSize,
            _ => throw new System.ArgumentOutOfRangeException(nameof(variant), variant, null)
        };

        // nopCommerce materializes and stores the correctly-sized thumbnail through its configured
        // media provider (filesystem/Azure/etc.) and returns its CDN-aware URL.
        return await _pictureService.GetPictureUrlAsync(pictureId, targetSize, showDefaultPicture: true);
    }
}
