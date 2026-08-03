using System.Threading;
using System.Threading.Tasks;

namespace TwinParticles.CheckEngine.Domain.Images;

public interface IImageStorageService
{
    Task<int?> DownloadAndCreatePictureAsync(string url, string seoName, string altText, string titleText, CancellationToken cancellationToken);

    Task<int?> GetDefaultPlaceholderPictureIdAsync(CancellationToken cancellationToken);

    Task<bool> ReplacePictureBinaryAsync(int pictureId, string sourceUrl, string seoName, string altText, string titleText, CancellationToken cancellationToken);
}
