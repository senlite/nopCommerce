using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace TwinParticles.CheckEngine.Domain.Marketplace;

public interface IVendorOwnershipStore
{
    Task AssignProductAsync(int vendorId, int productId, CancellationToken cancellationToken);

    Task<int?> GetProductVendorIdAsync(int productId, CancellationToken cancellationToken);

    Task<IReadOnlyList<int>> GetProductIdsAsync(int vendorId, CancellationToken cancellationToken);

    Task<IReadOnlyList<int>> GetAllMappedProductIdsAsync(CancellationToken cancellationToken);

    Task<int> AssignUnmappedProductsAsync(int operatorVendorId, IReadOnlyList<int> productIds, CancellationToken cancellationToken);
}
