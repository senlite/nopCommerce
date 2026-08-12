using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Nop.Core.Domain.Catalog;
using Nop.Services.Catalog;
using TwinParticles.CheckEngine.Domain.ImportPipeline;
using TwinParticles.CheckEngine.Domain.Oem;

namespace TwinParticles.CheckEngine.Infrastructure.ImportPipeline;

/// <summary>
/// Creates or updates a simple nopCommerce product from an import row and links OEM maps.
/// </summary>
public sealed class NopImportProductPublisher : IImportProductPublisher
{
    private readonly IProductService _productService;
    private readonly IProductOemMapRepository _productOemMapRepository;

    public NopImportProductPublisher(IProductService productService, IProductOemMapRepository productOemMapRepository)
    {
        _productService = productService;
        _productOemMapRepository = productOemMapRepository;
    }

    public async Task<ImportProductPublishResult> PublishAsync(
        IReadOnlyDictionary<string, string?> fields,
        int? matchedOemNumberId,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        fields.TryGetValue("name", out var name);
        fields.TryGetValue("sku", out var sku);
        fields.TryGetValue("productId", out var productIdRaw);
        fields.TryGetValue("price", out var priceRaw);

        if (string.IsNullOrWhiteSpace(name) && string.IsNullOrWhiteSpace(sku) && !int.TryParse(productIdRaw, out _))
            return ImportProductPublishResult.Fail("import.publish.missing_identity");

        Product? product = null;
        if (int.TryParse(productIdRaw, out var existingId) && existingId > 0)
            product = await _productService.GetProductByIdAsync(existingId);

        if (product is null && !string.IsNullOrWhiteSpace(sku))
            product = await _productService.GetProductBySkuAsync(sku.Trim());

        var created = product is null;
        product ??= new Product
        {
            CreatedOnUtc = DateTime.UtcNow,
            ProductType = ProductType.SimpleProduct,
            VisibleIndividually = true,
            Published = true,
            OrderMinimumQuantity = 1,
            OrderMaximumQuantity = 10000
        };

        product.Name = string.IsNullOrWhiteSpace(name) ? product.Name : name!.Trim();
        if (string.IsNullOrWhiteSpace(product.Name))
            product.Name = !string.IsNullOrWhiteSpace(sku) ? sku!.Trim() : $"Imported part {DateTime.UtcNow:yyyyMMddHHmmss}";

        if (!string.IsNullOrWhiteSpace(sku))
            product.Sku = sku.Trim();

        if (decimal.TryParse(priceRaw, out var price))
            product.Price = price;

        product.UpdatedOnUtc = DateTime.UtcNow;

        if (created)
            await _productService.InsertProductAsync(product);
        else
            await _productService.UpdateProductAsync(product);

        if (matchedOemNumberId is > 0)
        {
            await _productOemMapRepository.UpsertAsync(new ProductOemMap
            {
                ProductId = product.Id,
                OemNumberId = matchedOemNumberId.Value,
                IsPrimary = true,
                CreatedUtc = DateTime.UtcNow
            }, cancellationToken);
        }

        return ImportProductPublishResult.Ok(product.Id);
    }
}
