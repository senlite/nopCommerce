using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Nop.Core;
using Nop.Services.Media;
using TwinParticles.CheckEngine.Domain.Images;

namespace TwinParticles.CheckEngine.Infrastructure.Images;

public sealed class NopPictureImageStorageService : IImageStorageService
{
    private static readonly HttpClient HttpClient = new();
    private readonly IPictureService _pictureService;

    public NopPictureImageStorageService(IPictureService pictureService)
    {
        _pictureService = pictureService;
    }

    public async Task<int?> DownloadAndCreatePictureAsync(string url, string seoName, string altText, string titleText, CancellationToken cancellationToken)
    {
        try
        {
            var bytes = await HttpClient.GetByteArrayAsync(url, cancellationToken);
            if (bytes.Length == 0)
                return null;

            var mime = ResolveMimeFromUrl(url);
            var picture = await _pictureService.InsertPictureAsync(bytes, mime, seoName, altText, titleText);
            return picture?.Id;
        }
        catch
        {
            return null;
        }
    }

    public async Task<int?> GetDefaultPlaceholderPictureIdAsync(CancellationToken cancellationToken)
    {
        var picture = await _pictureService.InsertPictureAsync(Array.Empty<byte>(), MimeTypes.ImagePng, "checkengine-placeholder", "Placeholder", "Placeholder");
        return picture?.Id;
    }

    public async Task<bool> ReplacePictureBinaryAsync(int pictureId, string sourceUrl, string seoName, string altText, string titleText, CancellationToken cancellationToken)
    {
        try
        {
            var bytes = await HttpClient.GetByteArrayAsync(sourceUrl, cancellationToken);
            if (bytes.Length == 0)
                return false;

            var mime = ResolveMimeFromUrl(sourceUrl);
            var updated = await _pictureService.UpdatePictureAsync(pictureId, bytes, mime, seoName, altText, titleText, false, false);
            return updated is not null;
        }
        catch
        {
            return false;
        }
    }

    private static string ResolveMimeFromUrl(string url)
    {
        var extension = Path.GetExtension(url)?.ToLowerInvariant();
        return extension switch
        {
            ".jpg" => MimeTypes.ImageJpeg,
            ".jpeg" => MimeTypes.ImageJpeg,
            ".gif" => MimeTypes.ImageGif,
            ".webp" => MimeTypes.ImageWebp,
            ".svg" => MimeTypes.ImageSvg,
            _ => MimeTypes.ImagePng
        };
    }
}
