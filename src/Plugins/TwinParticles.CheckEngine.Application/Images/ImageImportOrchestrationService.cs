using System.Threading;
using System.Threading.Tasks;
using TwinParticles.CheckEngine.Application.ImportPipeline.Orchestration;
using TwinParticles.CheckEngine.Domain.Images;

namespace TwinParticles.CheckEngine.Application.Images;

public sealed class ImageImportOrchestrationService
{
    private readonly ProductImageService _productImageService;

    public ImageImportOrchestrationService(ProductImageService productImageService)
    {
        _productImageService = productImageService;
    }

    public async Task ApplyAsync(ImportPipelineRowState row, CancellationToken cancellationToken)
    {
        if (!row.IsPublished)
            return;

        if (!row.Fields.TryGetValue("publishedProductId", out var productIdRaw) ||
            !int.TryParse(productIdRaw, out var productId) ||
            productId <= 0)
        {
            row.PublishError = "import.image.product_missing";
            return;
        }

        var result = await _productImageService.AssignFromImportAsync(new ImageImportRequest
        {
            ProductId = productId,
            SourceUrl = row.ImageUrl,
            FallbackSeoName = row.Fields.TryGetValue("name", out var name) && !string.IsNullOrWhiteSpace(name)
                ? name!
                : $"row-{row.RowNumber}",
            AltTextEn = row.Fields.TryGetValue("alt_en", out var altEn) ? altEn : null,
            AltTextAr = row.Fields.TryGetValue("alt_ar", out var altAr) ? altAr : null
        }, cancellationToken);

        if (!result.Success)
            row.PublishError = result.ErrorCode;
    }
}
