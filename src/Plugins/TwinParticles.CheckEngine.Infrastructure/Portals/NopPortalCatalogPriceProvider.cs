using System.Threading;
using System.Threading.Tasks;
using Nop.Services.Catalog;
using TwinParticles.CheckEngine.Domain.Portals;

namespace TwinParticles.CheckEngine.Infrastructure.Portals;

public sealed class NopPortalCatalogPriceProvider : IPortalCatalogPriceProvider
{
    private readonly IProductService _productService;

    public NopPortalCatalogPriceProvider(IProductService productService)
    {
        _productService = productService;
    }

    public async Task<decimal> GetProductPriceAsync(int productId, CancellationToken cancellationToken)
    {
        var product = await _productService.GetProductByIdAsync(productId);
        return product?.Price ?? 0m;
    }
}
