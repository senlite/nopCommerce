using System.Threading;
using System.Threading.Tasks;

namespace TwinParticles.CheckEngine.Domain.Images;

/// <summary>Resolves a supplier SKU to a nopCommerce product id for batch image operations.</summary>
public interface IProductLookupService
{
    Task<int?> ResolveProductIdBySkuAsync(string sku, CancellationToken cancellationToken);
}
