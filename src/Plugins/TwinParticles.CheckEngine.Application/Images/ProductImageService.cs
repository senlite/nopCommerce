using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TwinParticles.CheckEngine.Domain.Images;

namespace TwinParticles.CheckEngine.Application.Images;

public sealed class ProductImageService
{
    private readonly IImageDeliveryService _deliveryService;
    private readonly IImageQuarantineService _quarantineService;
    private readonly IImageStorageService _storageService;
    private readonly IProductImageRepository _repository;

    public ProductImageService(
        IImageStorageService storageService,
        IImageDeliveryService deliveryService,
        IImageQuarantineService quarantineService,
        IProductImageRepository repository)
    {
        _storageService = storageService;
        _deliveryService = deliveryService;
        _quarantineService = quarantineService;
        _repository = repository;
    }

    public async Task<ImageImportResult> AssignFromImportAsync(ImageImportRequest request, CancellationToken cancellationToken)
    {
        if (request.ProductId <= 0)
            return new ImageImportResult { Success = false, ErrorCode = "image.invalid_product" };

        var sourceUrl = request.SourceUrl?.Trim();
        var alt = string.IsNullOrWhiteSpace(request.AltTextEn) ? request.FallbackSeoName : request.AltTextEn!.Trim();
        var title = string.IsNullOrWhiteSpace(request.AltTextAr) ? alt : request.AltTextAr!.Trim();
        var seoName = string.IsNullOrWhiteSpace(request.FallbackSeoName) ? $"product-{request.ProductId}" : request.FallbackSeoName.Trim();

        if (!string.IsNullOrWhiteSpace(sourceUrl) && await _quarantineService.ShouldQuarantineAsync(sourceUrl, cancellationToken))
        {
            await _repository.UpsertPrimaryAsync(new ProductImageRecord
            {
                ProductId = request.ProductId,
                PictureId = 0,
                SourceUrl = sourceUrl,
                IsPlaceholder = false,
                QuarantineStatus = QuarantineStatus.Quarantined,
                CreatedUtc = DateTime.UtcNow
            }, cancellationToken);

            return new ImageImportResult
            {
                Success = false,
                Quarantined = true,
                ErrorCode = "image.quarantined"
            };
        }

        int? pictureId = null;
        var usedPlaceholder = false;

        if (!string.IsNullOrWhiteSpace(sourceUrl))
            pictureId = await _storageService.DownloadAndCreatePictureAsync(sourceUrl, seoName, alt, title, cancellationToken);

        if (!pictureId.HasValue || pictureId.Value <= 0)
        {
            pictureId = await _storageService.GetDefaultPlaceholderPictureIdAsync(cancellationToken);
            usedPlaceholder = true;
        }

        if (!pictureId.HasValue || pictureId.Value <= 0)
            return new ImageImportResult { Success = false, ErrorCode = "image.create_failed" };

        await _repository.UpsertPrimaryAsync(new ProductImageRecord
        {
            ProductId = request.ProductId,
            PictureId = pictureId.Value,
            SourceUrl = sourceUrl,
            IsPlaceholder = usedPlaceholder,
            QuarantineStatus = QuarantineStatus.None,
            CreatedUtc = DateTime.UtcNow
        }, cancellationToken);

        var variants = await GenerateVariantsAsync(pictureId.Value, cancellationToken);

        return new ImageImportResult
        {
            Success = true,
            PictureId = pictureId,
            UsedPlaceholder = usedPlaceholder,
            CdnUrl = variants.GetValueOrDefault(ImageVariant.Product),
            VariantUrls = variants
        };
    }

    public async Task<ImageImportResult> ReplacePrimaryAsync(int productId, string sourceUrl, string fallbackSeoName, string? altTextEn, string? altTextAr, CancellationToken cancellationToken)
    {
        if (productId <= 0 || string.IsNullOrWhiteSpace(sourceUrl))
            return new ImageImportResult { Success = false, ErrorCode = "image.invalid_request" };

        if (await _quarantineService.ShouldQuarantineAsync(sourceUrl, cancellationToken))
            return new ImageImportResult { Success = false, Quarantined = true, ErrorCode = "image.quarantined" };

        var existing = await _repository.GetPrimaryAsync(productId, cancellationToken);
        if (existing is null || existing.PictureId <= 0)
            return await AssignFromImportAsync(new ImageImportRequest
            {
                ProductId = productId,
                SourceUrl = sourceUrl,
                FallbackSeoName = fallbackSeoName,
                AltTextEn = altTextEn,
                AltTextAr = altTextAr
            }, cancellationToken);

        var alt = string.IsNullOrWhiteSpace(altTextEn) ? fallbackSeoName : altTextEn!.Trim();
        var title = string.IsNullOrWhiteSpace(altTextAr) ? alt : altTextAr!.Trim();
        var ok = await _storageService.ReplacePictureBinaryAsync(existing.PictureId, sourceUrl, fallbackSeoName, alt, title, cancellationToken);
        if (!ok)
            return new ImageImportResult { Success = false, ErrorCode = "image.replace_failed" };

        existing.SourceUrl = sourceUrl;
        existing.IsPlaceholder = false;
        existing.QuarantineStatus = QuarantineStatus.None;
        await _repository.UpsertPrimaryAsync(existing, cancellationToken);

        var variants = await GenerateVariantsAsync(existing.PictureId, cancellationToken);
        return new ImageImportResult
        {
            Success = true,
            PictureId = existing.PictureId,
            UsedPlaceholder = false,
            CdnUrl = variants.GetValueOrDefault(ImageVariant.Product),
            VariantUrls = variants
        };
    }

    private async Task<IReadOnlyDictionary<ImageVariant, string>> GenerateVariantsAsync(
        int pictureId,
        CancellationToken cancellationToken)
    {
        var result = new Dictionary<ImageVariant, string>();
        foreach (var variant in Enum.GetValues<ImageVariant>())
        {
            var url = await _deliveryService.GetVariantUrlAsync(pictureId, variant, cancellationToken);
            if (!string.IsNullOrWhiteSpace(url))
                result[variant] = url;
        }

        return result;
    }
}
