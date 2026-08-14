using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TwinParticles.CheckEngine.Domain.Images;
using TwinParticles.CheckEngine.Domain.Security;

namespace TwinParticles.CheckEngine.Application.Images;

/// <summary>
/// Queues professional image replacements by supplier SKU (doc 26 "Bulk"; FR-631). Each item is
/// resolved to a product and replaced in place so existing product/gallery links stay stable. One
/// bad row never blocks the rest; every outcome is reported per item and the batch is audited.
/// </summary>
public sealed class BatchImageReplacementService
{
    private readonly ProductImageService _productImageService;
    private readonly IProductLookupService _productLookup;
    private readonly ICheckEngineAuditService? _auditService;

    public BatchImageReplacementService(
        ProductImageService productImageService,
        IProductLookupService productLookup,
        ICheckEngineAuditService? auditService = null)
    {
        _productImageService = productImageService;
        _productLookup = productLookup;
        _auditService = auditService;
    }

    public async Task<BatchImageReplacementResult> ReplaceBySkuAsync(
        IReadOnlyList<BatchImageReplacementItem> items,
        string actor,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(items);

        var results = new List<BatchImageReplacementItemResult>(items.Count);
        foreach (var item in items)
        {
            var sku = item.Sku?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(sku) || string.IsNullOrWhiteSpace(item.SourceUrl))
            {
                results.Add(new BatchImageReplacementItemResult
                {
                    Sku = sku,
                    Status = BatchImageReplacementStatus.Failed,
                    ErrorCode = "image.batch.invalid_item"
                });
                continue;
            }

            int? productId;
            try
            {
                productId = await _productLookup.ResolveProductIdBySkuAsync(sku, cancellationToken);
            }
            catch
            {
                productId = null;
            }

            if (!productId.HasValue || productId.Value <= 0)
            {
                results.Add(new BatchImageReplacementItemResult
                {
                    Sku = sku,
                    Status = BatchImageReplacementStatus.SkuNotFound,
                    ErrorCode = "image.batch.sku_not_found"
                });
                continue;
            }

            var seoName = string.IsNullOrWhiteSpace(item.SeoName) ? sku : item.SeoName.Trim();
            var replacement = await _productImageService.ReplacePrimaryAsync(
                productId.Value, item.SourceUrl.Trim(), seoName, item.AltTextEn, item.AltTextAr, cancellationToken);

            var status = replacement.Success
                ? BatchImageReplacementStatus.Replaced
                : replacement.Quarantined
                    ? BatchImageReplacementStatus.Quarantined
                    : BatchImageReplacementStatus.Failed;

            results.Add(new BatchImageReplacementItemResult
            {
                Sku = sku,
                ProductId = productId.Value,
                Status = status,
                ErrorCode = replacement.Success ? null : replacement.ErrorCode
            });
        }

        var result = new BatchImageReplacementResult { Items = results };

        if (_auditService is not null)
        {
            await _auditService.AppendAsync(
                actor,
                "image.batch_replace",
                "ProductImage",
                $"count:{items.Count}",
                beforeJson: null,
                afterJson: $"{{\"replaced\":{result.Replaced},\"quarantined\":{result.Quarantined},\"notFound\":{result.NotFound},\"failed\":{result.Failed}}}",
                cancellationToken);
        }

        return result;
    }
}

public sealed class BatchImageReplacementItem
{
    public required string Sku { get; init; }
    public required string SourceUrl { get; init; }
    public string? SeoName { get; init; }
    public string? AltTextEn { get; init; }
    public string? AltTextAr { get; init; }
}
