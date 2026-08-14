using System.Threading;
using System.Threading.Tasks;
using Nop.Services.Catalog;
using TwinParticles.CheckEngine.Domain.Images;

namespace TwinParticles.CheckEngine.Infrastructure.Images;

public sealed class NopProductLookupService : IProductLookupService
{
    private readonly IProductService _productService;

    public NopProductLookupService(IProductService productService)
    {
        _productService = productService;
    }

    public async Task<int?> ResolveProductIdBySkuAsync(string sku, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(sku))
            return null;

        var product = await _productService.GetProductBySkuAsync(sku.Trim());
        return product is { Deleted: false } ? product.Id : null;
    }
}
